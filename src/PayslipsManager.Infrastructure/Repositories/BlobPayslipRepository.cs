using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayslipsManager.Application.Interfaces;
using PayslipsManager.Domain.Entities;
using PayslipsManager.Domain.Enums;
using PayslipsManager.Infrastructure.Configuration;

namespace PayslipsManager.Infrastructure.Repositories;

/// <summary>
/// Implements payslip storage operations against Azure Blob Storage.
/// Each employee has their own container (named by employee id).
/// </summary>
public class BlobPayslipRepository : IPayslipStorageService
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

        if (_options.UseManagedIdentity && !string.IsNullOrEmpty(_options.AccountUrl))
        {
            _blobServiceClient = new BlobServiceClient(new Uri(_options.AccountUrl), new DefaultAzureCredential());
        }
        else if (!string.IsNullOrEmpty(_options.ConnectionString))
        {
            _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
        }
        else
        {
            throw new InvalidOperationException(
                "BlobStorage configuration is invalid. Provide either AccountUrl with managed identity or ConnectionString.");
        }
    }

    public async Task<IReadOnlyList<PayslipDocument>> ListPayslipsAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing payslips for employee {EmployeeId}", employeeId);

        var containerClient = _blobServiceClient.GetBlobContainerClient(employeeId);
        var documents = new List<PayslipDocument>();

        await foreach (var blobItem in containerClient.GetBlobsAsync(
            traits: BlobTraits.Metadata | BlobTraits.Tags,
            states: BlobStates.None,
            prefix: null,
            cancellationToken: cancellationToken))
        {
            documents.Add(MapBlobItemToDocument(blobItem, employeeId));
        }

        _logger.LogInformation("Found {Count} payslips for employee {EmployeeId}", documents.Count, employeeId);
        return documents.AsReadOnly();
    }

    public async Task<PayslipDocument?> GetPayslipAsync(string employeeId, string blobName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payslip {BlobName} for employee {EmployeeId}", blobName, employeeId);

        var containerClient = _blobServiceClient.GetBlobContainerClient(employeeId);
        var blobClient = containerClient.GetBlobClient(blobName);

        try
        {
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var tags = await blobClient.GetTagsAsync(cancellationToken: cancellationToken);

            return MapPropertiesToDocument(blobName, employeeId, properties.Value, tags.Value.Tags);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Payslip not found: {BlobName}", blobName);
            return null;
        }
    }

    public async Task<Stream?> DownloadContentAsync(string employeeId, string blobName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading {BlobName} from container {EmployeeId}", blobName, employeeId);

        var containerClient = _blobServiceClient.GetBlobContainerClient(employeeId);
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

    public async Task<string?> GenerateSasUrlAsync(string employeeId, string blobName, TimeSpan validity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating SAS URL for {BlobName} in container {EmployeeId}", blobName, employeeId);

        var containerClient = _blobServiceClient.GetBlobContainerClient(employeeId);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Blob not found: {BlobName}", blobName);
            return null;
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = employeeId,
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

            var sasToken = sasBuilder.ToSasQueryParameters(userDelegationKey.Value, _blobServiceClient.AccountName).ToString();
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
        var year = int.TryParse(GetTagValue(tags, "Year"), out var y) ? y : 0;
        var month = int.TryParse(GetTagValue(tags, "Month"), out var m) ? m : 1;

        return new PayslipDocument
        {
            BlobName = blobItem.Name,
            FileName = Path.GetFileName(blobItem.Name),
            EmployeeId = employeeId,
            PayslipDate = new DateOnly(year, month, 1),
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
        var year = int.TryParse(GetTagValue(tags, "Year"), out var y) ? y : 0;
        var month = int.TryParse(GetTagValue(tags, "Month"), out var m) ? m : 1;

        return new PayslipDocument
        {
            BlobName = blobName,
            FileName = Path.GetFileName(blobName),
            EmployeeId = employeeId,
            PayslipDate = new DateOnly(year, month, 1),
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
            "Archive" => BlobAccessTier.Archive,
            _ => BlobAccessTier.Hot
        };

    private static string GetTagValue(IDictionary<string, string> tags, string key) =>
        tags.TryGetValue(key, out var value) ? value : string.Empty;
}
