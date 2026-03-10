using Microsoft.Extensions.Logging;
using PayslipsManager.Application.DTOs;
using PayslipsManager.Application.Interfaces;
using PayslipsManager.Domain.Entities;
using PayslipsManager.Domain.Enums;

namespace PayslipsManager.Application.Services;

/// <summary>
/// Implements query and download operations for payslips.
/// </summary>
public class PayslipService : IPayslipQueryService, IPayslipDownloadService
{
    private readonly IPayslipStorageService _storage;
    private readonly ILogger<PayslipService> _logger;

    public PayslipService(IPayslipStorageService storage, ILogger<PayslipService> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<PayslipListItemDto>> GetPayslipsForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId);

        _logger.LogDebug("Retrieving payslips for employee {EmployeeId}", employeeId);

        var documents = await _storage.ListPayslipsAsync(employeeId, cancellationToken);

        return documents
            .OrderByDescending(d => d.PayslipDate)
            .Select(MapToListItem)
            .ToList()
            .AsReadOnly();
    }

    public async Task<PayslipDetailsDto?> GetPayslipDetailsAsync(string employeeId, string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        _logger.LogDebug("Retrieving payslip details {BlobName} for employee {EmployeeId}", blobName, employeeId);

        var doc = await _storage.GetPayslipAsync(employeeId, blobName, cancellationToken);
        if (doc is null)
        {
            _logger.LogWarning("Payslip {BlobName} not found for employee {EmployeeId}", blobName, employeeId);
            return null;
        }

        return MapToDetails(doc);
    }

    public async Task<Stream?> DownloadAsync(string employeeId, string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        _logger.LogInformation("Payslip download initiated -- Employee: {EmployeeId}, Blob: {BlobName}", employeeId, blobName);

        return await _storage.DownloadContentAsync(employeeId, blobName, cancellationToken);
    }

    public async Task<string?> GenerateDownloadUrlAsync(string employeeId, string blobName, TimeSpan validity, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        _logger.LogDebug("Generating SAS download URL -- Employee: {EmployeeId}, Blob: {BlobName}", employeeId, blobName);

        return await _storage.GenerateSasUrlAsync(employeeId, blobName, validity, cancellationToken);
    }

    // ── Mapping helpers ──────────────────────────────────────────────

    private static PayslipListItemDto MapToListItem(PayslipDocument doc) => new()
    {
        BlobName = doc.BlobName,
        FileName = doc.FileName,
        PayslipDate = doc.PayslipDate,
        Period = doc.GetPeriodDisplay(),
        AccessTier = doc.AccessTier.ToString(),
        IsArchived = doc.IsArchived,
        UploadedOn = doc.UploadedOn
    };

    private static PayslipDetailsDto MapToDetails(PayslipDocument doc)
    {
        var dto = new PayslipDetailsDto
        {
            BlobName = doc.BlobName,
            FileName = doc.FileName,
            EmployeeId = doc.EmployeeId,
            PayslipDate = doc.PayslipDate,
            Period = doc.GetPeriodDisplay(),
            AccessTier = doc.AccessTier.ToString(),
            IsArchived = doc.IsArchived,
            UploadedOn = doc.UploadedOn,
            ContentType = doc.ContentType,
            Tags = doc.Tags
        };

        if (doc.IsArchived)
        {
            dto.ArchiveWarning = new ArchiveWarningDto
            {
                IsArchived = true,
                CurrentTier = doc.AccessTier.ToString(),
                Message = "This payslip is in the Archive tier. Downloading it may take several hours while the blob is rehydrated."
            };
        }

        return dto;
    }
}
