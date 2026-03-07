using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PayslipsManager.Application.Interfaces;

namespace PayslipsManager.Functions;

/// <summary>
/// Azure Function triggered when a new payslip blob is created.
/// Each employee has a dedicated container (payslips-{employeeId}).
/// Validates the blob and sets metadata / blob index tags via IPayslipEventProcessor.
/// </summary>
public class PayslipBlobTriggerFunction
{
    private readonly IPayslipEventProcessor _eventProcessor;
    private readonly IPayslipStorageService _storageService;
    private readonly ILogger<PayslipBlobTriggerFunction> _logger;

    public PayslipBlobTriggerFunction(
        IPayslipEventProcessor eventProcessor,
        IPayslipStorageService storageService,
        ILogger<PayslipBlobTriggerFunction> logger)
    {
        _eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Triggered when a new blob is created in any payslips-* container.
    /// The {container} segment captures the full container name, and {name} captures the blob name.
    /// </summary>
    [Function(nameof(PayslipBlobTriggerFunction))]
    public async Task Run(
        [BlobTrigger("{container}/{name}", Connection = "BlobStorage:ConnectionString")] BlobClient blobClient,
        string container,
        string name,
        FunctionContext context)
    {
        _logger.LogInformation("New blob detected: {BlobName} in container {Container}", name, container);

        // Extract the employee ID from the container name by removing the prefix
        var employeeId = ResolveEmployeeIdFromContainer(container);
        if (string.IsNullOrEmpty(employeeId))
        {
            _logger.LogWarning("Could not resolve employee ID from container {Container}. Skipping.", container);
            return;
        }

        try
        {
            // Download content for validation
            using var contentStream = new MemoryStream();
            await blobClient.DownloadToAsync(contentStream);
            contentStream.Position = 0;

            var result = await _eventProcessor.ProcessNewPayslipAsync(employeeId, name, contentStream);

            if (result.IsValid)
            {
                _logger.LogInformation("Payslip {BlobName} processed successfully for employee {EmployeeId}",
                    name, employeeId);
            }
            else
            {
                _logger.LogWarning("Payslip {BlobName} validation failed: {Errors}",
                    name, string.Join("; ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing blob {BlobName} in container {Container}: {ErrorMessage}",
                name, container, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Extracts the employee ID from a container name like "payslips-{employeeId}".
    /// </summary>
    private static string? ResolveEmployeeIdFromContainer(string containerName)
    {
        const string prefix = "payslips-";
        if (containerName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            containerName.Length > prefix.Length)
        {
            return containerName[prefix.Length..];
        }

        // If no prefix, assume the container name IS the employee ID
        return containerName;
    }
}
