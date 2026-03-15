using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayslipsManager.Application.Interfaces;
using PayslipsManager.Domain.Entities;
using PayslipsManager.Domain.Enums;
using PayslipsManager.Infrastructure.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PayslipsManager.Infrastructure.Repositories;

/// <summary>
/// Implements payslip storage operations against Azure Blob Storage.
/// Each employee has a dedicated container named {ContainerPrefix}-{employeeId}.
/// Blob naming convention: yyyy-MM.pdf
/// </summary>
public partial class BlobPayslipRepository : IPayslipStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<BlobPayslipRepository> _logger;

    /// <summary>
    /// Matches valid payslip blob names: yyyy-MM.pdf (e.g. 2026-03.pdf).
    /// </summary>
    [GeneratedRegex(@"^\d{4}-(0[1-9]|1[0-2])\.pdf$")]
    private static partial Regex BlobNameRegex();

    public BlobPayslipRepository(
        BlobServiceClient blobServiceClient,
        IOptions<BlobStorageOptions> options,
        ILogger<BlobPayslipRepository> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Container resolution ─────────────────────────────────────────

    /// <inheritdoc />
    public string ResolveContainerName(string employeeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId);

        var sanitized = employeeId.ToLowerInvariant();
        return sanitized;
    }

    // ── Validation ───────────────────────────────────────────────────

    /// <inheritdoc />
    public PayslipValidationResult ValidateBlobName(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            return PayslipValidationResult.Failure("Blob name cannot be empty.");

        if (!BlobNameRegex().IsMatch(blobName))
            return PayslipValidationResult.Failure(
                $"Blob name '{blobName}' does not match the required pattern yyyy-MM.pdf.");

        return PayslipValidationResult.Success();
    }

    /// <inheritdoc />
    public PayslipValidationResult ValidateContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return PayslipValidationResult.Failure("Content type cannot be empty.");

        if (!string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return PayslipValidationResult.Failure(
                $"Content type '{contentType}' is not supported. Only application/pdf is allowed.");

        return PayslipValidationResult.Success();
    }

    // ── Container management ─────────────────────────────────────────

    /// <inheritdoc />
    public async Task EnsureContainerExistsAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        var containerName = ResolveContainerName(employeeId);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        _logger.LogInformation("Ensured container {ContainerName} exists for employee {EmployeeId}",
            containerName, employeeId);
    }

    // ── List ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<PayslipDocument>> ListPayslipsAsync(
        string employeeId, CancellationToken cancellationToken = default)
    {
        var containerName = ResolveContainerName(employeeId);
        _logger.LogDebug("Listing payslips in container {ContainerName}", containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var documents = new List<PayslipDocument>();

        try
        {
            await foreach (var blobItem in containerClient.GetBlobsAsync(
                traits: BlobTraits.Metadata | BlobTraits.Tags,
                states: BlobStates.None,
                prefix: null,
                cancellationToken: cancellationToken))
            {
                documents.Add(MapBlobItemToDocument(blobItem, employeeId));
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Container {ContainerName} not found for employee {EmployeeId}",
                containerName, employeeId);
            return documents.AsReadOnly();
        }

        _logger.LogDebug("Found {Count} payslips in container {ContainerName}",
            documents.Count, containerName);
        return documents.AsReadOnly();
    }

    // ── Get ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<PayslipDocument?> GetPayslipAsync(
        string employeeId, string blobName, CancellationToken cancellationToken = default)
    {
        var containerName = ResolveContainerName(employeeId);
        _logger.LogDebug("Getting payslip {BlobName} from container {ContainerName}",
            blobName, containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        try
        {
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var tags = await blobClient.GetTagsAsync(cancellationToken: cancellationToken);
            return MapPropertiesToDocument(blobName, employeeId, properties.Value, tags.Value.Tags);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Payslip {BlobName} not found in container {ContainerName}",
                blobName, containerName);
            return null;
        }
    }

    // ── Download ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Stream?> DownloadContentAsync(
        string employeeId, string blobName, CancellationToken cancellationToken = default)
    {
        var containerName = ResolveContainerName(employeeId);
        _logger.LogInformation("Downloading {BlobName} from container {ContainerName}",
            blobName, containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        try
        {
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return response.Value.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Blob {BlobName} not found for download in container {ContainerName}",
                blobName, containerName);
            return null;
        }
    }

    // ── Upload ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task UploadPayslipAsync(
        string employeeId, string blobName, Stream content,
        IDictionary<string, string> metadata, IDictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        var containerName = ResolveContainerName(employeeId);
        _logger.LogInformation("Uploading {BlobName} to container {ContainerName}",
            blobName, containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/pdf" },
            Metadata = new Dictionary<string, string>(metadata),
            Tags = new Dictionary<string, string>(tags)
        };

        await blobClient.UploadAsync(content, uploadOptions, cancellationToken);
        _logger.LogInformation("Uploaded payslip {BlobName} to container {ContainerName}",
            blobName, containerName);
    }

    // ── Tags ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task SetTagsAsync(
        string employeeId, string blobName, IDictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        var containerName = ResolveContainerName(employeeId);
        _logger.LogInformation("Setting tags on {BlobName} in container {ContainerName}",
            blobName, containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.SetTagsAsync(tags, cancellationToken: cancellationToken);

        _logger.LogInformation("Tags set on {BlobName} in container {ContainerName}",
            blobName, containerName);
    }

    // ── SAS URL ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<string?> GenerateSasUrlAsync(
        string employeeId, string blobName, TimeSpan validity,
        CancellationToken cancellationToken = default)
    {
        var containerName = ResolveContainerName(employeeId);
        _logger.LogDebug("Generating SAS URL for {BlobName} in container {ContainerName}",
            blobName, containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Blob {BlobName} not found in container {ContainerName}",
                blobName, containerName);
            return null;
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.Add(validity)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        if (_options.UseManagedIdentity)
        {
            var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(
                startsOn: sasBuilder.StartsOn,
                expiresOn: sasBuilder.ExpiresOn,
                cancellationToken: cancellationToken);

            var sasToken = sasBuilder
                .ToSasQueryParameters(userDelegationKey.Value, _blobServiceClient.AccountName)
                .ToString();
            return $"{blobClient.Uri}?{sasToken}";
        }
        else
        {
            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }
    }

    // ── Mapping helpers ──────────────────────────────────────────────

    private static PayslipDocument MapBlobItemToDocument(BlobItem blobItem, string employeeId)
    {
        var tags = blobItem.Tags ?? new Dictionary<string, string>();

        var year = int.TryParse(GetTagValue(tags, PayslipTagKeys.PayslipYear), out var y) ? y : 0;
        var month = int.TryParse(GetTagValue(tags, PayslipTagKeys.PayslipMonth), out var m) ? m : 0;

        // Fallback: parse date from blob name if tags are missing
        if (year == 0 || month == 0)
            (year, month) = ParseBlobNameDate(blobItem.Name);

        return new PayslipDocument
        {
            BlobName = blobItem.Name,
            FileName = Path.GetFileName(blobItem.Name),
            EmployeeId = GetTagValue(tags, PayslipTagKeys.EmployeeId, employeeId),
            PayslipDate = new DateOnly(year == 0 ? 1 : year, month == 0 ? 1 : month, 1),
            AccessTier = ParseAccessTier(blobItem.Properties.AccessTier?.ToString()),
            UploadedOn = blobItem.Properties.CreatedOn ?? DateTimeOffset.UtcNow,
            ContentType = blobItem.Properties.ContentType ?? "application/pdf",
            Tags = new Dictionary<string, string>(tags)
        };
    }

    private static PayslipDocument MapPropertiesToDocument(
        string blobName, string employeeId,
        BlobProperties properties, IDictionary<string, string> tags)
    {
        var year = int.TryParse(GetTagValue(tags, PayslipTagKeys.PayslipYear), out var y) ? y : 0;
        var month = int.TryParse(GetTagValue(tags, PayslipTagKeys.PayslipMonth), out var m) ? m : 0;

        if (year == 0 || month == 0)
            (year, month) = ParseBlobNameDate(blobName);

        return new PayslipDocument
        {
            BlobName = blobName,
            FileName = Path.GetFileName(blobName),
            EmployeeId = GetTagValue(tags, PayslipTagKeys.EmployeeId, employeeId),
            PayslipDate = new DateOnly(year == 0 ? 1 : year, month == 0 ? 1 : month, 1),
            AccessTier = ParseAccessTier(properties.AccessTier?.ToString()),
            UploadedOn = properties.CreatedOn,
            ContentType = properties.ContentType ?? "application/pdf",
            Tags = new Dictionary<string, string>(tags)
        };
    }

    private static BlobAccessTier ParseAccessTier(string? tier) =>
        tier switch
        {
            "Cool" => BlobAccessTier.Cool,
            "Cold" => BlobAccessTier.Cold,
            "Archive" => BlobAccessTier.Archive,
            _ => BlobAccessTier.Hot
        };

    private static string GetTagValue(IDictionary<string, string> tags, string key, string defaultValue = "") =>
        tags.TryGetValue(key, out var value) ? value : defaultValue;

    /// <summary>
    /// Extracts year and month from a blob name like "2026-03.pdf".
    /// </summary>
    private static (int Year, int Month) ParseBlobNameDate(string blobName)
    {
        var name = Path.GetFileNameWithoutExtension(blobName);
        if (name.Length >= 7 &&
            int.TryParse(name.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year) &&
            name[4] == '-' &&
            int.TryParse(name.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var month))
        {
            return (year, month);
        }
        return (0, 0);
    }
}
