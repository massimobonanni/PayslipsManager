using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayslipsManager.Application.Interfaces;
using PayslipsManager.Infrastructure.Configuration;

namespace PayslipsManager.Functions;

/// <summary>
/// Azure Function triggered by Event Grid when a new blob is created in a
/// payslip employee container. Validates the blob and applies index tags
/// via <see cref="IPayslipEventProcessor"/>.
/// </summary>
public class PayslipBlobCreatedFunction
{
    private const string BlobCreatedEventType = "Microsoft.Storage.BlobCreated";

    private readonly IPayslipEventProcessor _eventProcessor;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<PayslipBlobCreatedFunction> _logger;

    public PayslipBlobCreatedFunction(
        IPayslipEventProcessor eventProcessor,
        IOptions<BlobStorageOptions> options,
        ILogger<PayslipBlobCreatedFunction> logger)
    {
        _eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes a BlobCreated event delivered by Azure Event Grid.
    /// Validates the blob and sets index tags for valid payslips.
    /// Invalid files are logged but never deleted automatically.
    /// </summary>
    [Function(nameof(PayslipBlobCreatedFunction))]
    public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        _logger.LogInformation(
            "Event received -- Type: {EventType}, Subject: {Subject}",
            eventGridEvent.EventType, eventGridEvent.Subject);

        // ── Step 1: Only process BlobCreated events ──────────────────
        if (!string.Equals(eventGridEvent.EventType, BlobCreatedEventType, StringComparison.Ordinal))
        {
            _logger.LogInformation("Ignoring event type {EventType}.", eventGridEvent.EventType);
            return;
        }

        // ── Step 2: Deserialize the storage event data ───────────────
        if (!eventGridEvent.TryGetSystemEventData(out var systemEvent) ||
            systemEvent is not StorageBlobCreatedEventData blobData)
        {
            _logger.LogWarning("Could not deserialize BlobCreated event data. Skipping.");
            return;
        }

        if (string.IsNullOrEmpty(blobData.Url))
        {
            _logger.LogWarning("BlobCreated event has no URL. Skipping.");
            return;
        }

        // ── Step 3: Extract container and blob name from the URL ─────
        if (!TryParseBlobUrl(blobData.Url, out var containerName, out var blobName))
        {
            _logger.LogWarning("Could not parse blob URL: {Url}. Skipping.", blobData.Url);
            return;
        }

        _logger.LogInformation(
            "Blob created -- Container: {Container}, Blob: {BlobName}, ContentType: {ContentType}",
            containerName, blobName, blobData.ContentType);

        // ── Step 4: Validate container belongs to a payslip employee ─
        var employeeId = ResolveEmployeeId(containerName);
        if (employeeId is null)
        {
            _logger.LogWarning(
                "Container '{Container}' does not match prefix '{Prefix}-'. Not a payslip container -- skipping.",
                containerName, _options.ContainerPrefix);
            return;
        }

        // ── Step 5: Delegate to the event processor ──────────────────
        try
        {
            var result = await _eventProcessor.ProcessNewPayslipAsync(employeeId, blobName);

            if (result.IsValid)
            {
                _logger.LogInformation(
                    "Payslip processed successfully -- Employee: {EmployeeId}, Blob: {BlobName}",
                    employeeId, blobName);
            }
            else
            {
                _logger.LogWarning(
                    "Payslip validation failed -- Employee: {EmployeeId}, Blob: {BlobName}, Errors: {Errors}",
                    employeeId, blobName, string.Join("; ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing blob {BlobName} in container {Container}: {Message}",
                blobName, containerName, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Extracts the employee ID from a container name like "{prefix}-{employeeId}".
    /// Returns <c>null</c> if the container does not match the expected pattern.
    /// </summary>
    private string? ResolveEmployeeId(string containerName)
    {
        var prefix = $"{_options.ContainerPrefix}-";
        if (containerName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            containerName.Length > prefix.Length)
        {
            return containerName[prefix.Length..];
        }
        return null;
    }

    /// <summary>
    /// Parses a blob URL to extract the container name and blob name.
    /// URL format: https://account.blob.core.windows.net/container/blobname.pdf
    /// </summary>
    private static bool TryParseBlobUrl(string url, out string containerName, out string blobName)
    {
        containerName = string.Empty;
        blobName = string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            return false;

        containerName = Uri.UnescapeDataString(segments[0]);
        blobName = Uri.UnescapeDataString(string.Join('/', segments[1..]));
        return true;
    }
}
