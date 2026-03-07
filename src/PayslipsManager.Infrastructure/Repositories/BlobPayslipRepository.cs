using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayslipsManager.Application.Interfaces;
using PayslipsManager.Domain.Entities;
using PayslipsManager.Infrastructure.Configuration;

namespace PayslipsManager.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for payslip blob storage using Azure Blob Storage.
/// </summary>
public class BlobPayslipRepository : IPayslipRepository
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<BlobPayslipRepository> _logger;

    public BlobPayslipRepository(
        IOptions<BlobStorageOptions> options,
        ILogger<BlobPayslipRepository> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize BlobServiceClient with managed identity or connection string
        if (_options.UseManagedIdentity && !string.IsNullOrEmpty(_options.AccountUrl))
        {
            _logger.LogInformation("Initializing BlobServiceClient with managed identity for: {AccountUrl}", _options.AccountUrl);
            _blobServiceClient = new BlobServiceClient(new Uri(_options.AccountUrl), new DefaultAzureCredential());
        }
        else if (!string.IsNullOrEmpty(_options.ConnectionString))
        {
            _logger.LogInformation("Initializing BlobServiceClient with connection string");
            _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
        }
        else
        {
            throw new InvalidOperationException("BlobStorage configuration is invalid. Provide either AccountUrl with managed identity or ConnectionString.");
        }
    }

    public async Task<IEnumerable<Payslip>> ListPayslipsAsync(string employeeEmail, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing payslips for employee: {EmployeeEmail}", employeeEmail);

        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var payslips = new List<Payslip>();

        // List blobs with the employee email as a prefix or tag filter
        await foreach (var blobItem in containerClient.GetBlobsAsync(
            traits: BlobTraits.Metadata | BlobTraits.Tags,
            states: BlobStates.None,
            prefix: null,
            cancellationToken: cancellationToken))
        {
            // Check if blob belongs to the employee (via tags or metadata)
            if (blobItem.Tags?.ContainsKey("EmployeeEmail") == true &&
                string.Equals(blobItem.Tags["EmployeeEmail"], employeeEmail, StringComparison.OrdinalIgnoreCase))
            {
                payslips.Add(MapBlobItemToPayslip(blobItem, _options.ContainerName));
            }
        }

        _logger.LogInformation("Found {Count} payslips for employee: {EmployeeEmail}", payslips.Count, employeeEmail);
        return payslips;
    }

    public async Task<Payslip?> GetPayslipAsync(string payslipId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payslip: {PayslipId}", payslipId);

        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobClient = containerClient.GetBlobClient(payslipId);

        try
        {
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var tags = await blobClient.GetTagsAsync(cancellationToken: cancellationToken);

            return new Payslip
            {
                Id = payslipId,
                BlobName = payslipId,
                ContainerName = _options.ContainerName,
                EmployeeEmail = GetTagValue(tags.Value.Tags, "EmployeeEmail"),
                EmployeeId = GetTagValue(tags.Value.Tags, "EmployeeId"),
                EmployeeName = GetTagValue(tags.Value.Tags, "EmployeeName"),
                Year = int.TryParse(GetTagValue(tags.Value.Tags, "Year"), out var year) ? year : 0,
                Month = int.TryParse(GetTagValue(tags.Value.Tags, "Month"), out var month) ? month : 0,
                FileSizeBytes = properties.Value.ContentLength,
                UploadedAt = properties.Value.CreatedOn,
                LastAccessedAt = properties.Value.LastAccessed,
                AccessTier = properties.Value.AccessTier?.ToString() ?? "Hot",
                Status = GetMetadataValue(properties.Value.Metadata, "Status", "Available")
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Payslip not found: {PayslipId}", payslipId);
            return null;
        }
    }

    public async Task<Stream?> DownloadPayslipContentAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading payslip content: {BlobName} from container: {ContainerName}", blobName, containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        try
        {
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return response.Value.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Blob not found for download: {BlobName}", blobName);
            return null;
        }
    }

    public async Task<string?> GenerateSasUrlAsync(string containerName, string blobName, TimeSpan validity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating SAS URL for: {BlobName} in container: {ContainerName}", blobName, containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        // Check if blob exists
        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Blob not found: {BlobName}", blobName);
            return null;
        }

        // Generate SAS token for read access
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.Add(validity)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        // If using managed identity, we need user delegation key
        if (_options.UseManagedIdentity)
        {
            var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(
                startsOn: sasBuilder.StartsOn,
                expiresOn: sasBuilder.ExpiresOn,
                cancellationToken: cancellationToken);

            var sasToken = sasBuilder.ToSasQueryParameters(userDelegationKey.Value, _blobServiceClient.AccountName).ToString();
            return $"{blobClient.Uri}?{sasToken}";
        }
        else
        {
            // For connection string authentication, use account key
            var sasToken = blobClient.GenerateSasUri(sasBuilder);
            return sasToken.ToString();
        }
    }

    public async Task UpdateLastAccessedAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating last accessed timestamp for: {BlobName}", blobName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        try
        {
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var metadata = properties.Value.Metadata;
            metadata["LastAccessedAt"] = DateTimeOffset.UtcNow.ToString("O");

            await blobClient.SetMetadataAsync(metadata, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Failed to update last accessed timestamp for: {BlobName}", blobName);
        }
    }

    private static Payslip MapBlobItemToPayslip(BlobItem blobItem, string containerName)
    {
        var tags = blobItem.Tags ?? new Dictionary<string, string>();

        return new Payslip
        {
            Id = blobItem.Name,
            BlobName = blobItem.Name,
            ContainerName = containerName,
            EmployeeEmail = GetTagValue(tags, "EmployeeEmail"),
            EmployeeId = GetTagValue(tags, "EmployeeId"),
            EmployeeName = GetTagValue(tags, "EmployeeName"),
            Year = int.TryParse(GetTagValue(tags, "Year"), out var year) ? year : 0,
            Month = int.TryParse(GetTagValue(tags, "Month"), out var month) ? month : 0,
            FileSizeBytes = blobItem.Properties.ContentLength ?? 0,
            UploadedAt = blobItem.Properties.CreatedOn ?? DateTimeOffset.UtcNow,
            LastAccessedAt = blobItem.Properties.LastAccessedOn,
            AccessTier = blobItem.Properties.AccessTier?.ToString() ?? "Hot",
            Status = GetMetadataValue(blobItem.Metadata, "Status", "Available")
        };
    }

    private static string GetTagValue(IDictionary<string, string> tags, string key)
    {
        return tags.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private static string GetMetadataValue(IDictionary<string, string> metadata, string key, string defaultValue)
    {
        if (metadata == null) return defaultValue;
        return metadata.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
