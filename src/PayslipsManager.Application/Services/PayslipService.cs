using Microsoft.Extensions.Logging;
using PayslipsManager.Application.DTOs;
using PayslipsManager.Application.Interfaces;
using PayslipsManager.Domain.Entities;

namespace PayslipsManager.Application.Services;

/// <summary>
/// Application service for managing payslips.
/// </summary>
public class PayslipService : IPayslipService
{
    private readonly IPayslipRepository _repository;
    private readonly ILogger<PayslipService> _logger;

    public PayslipService(IPayslipRepository repository, ILogger<PayslipService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<PayslipDto>> GetPayslipsForEmployeeAsync(string employeeEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new ArgumentException("Employee email cannot be null or empty.", nameof(employeeEmail));
        }

        _logger.LogInformation("Retrieving payslips for employee: {EmployeeEmail}", employeeEmail);

        var payslips = await _repository.ListPayslipsAsync(employeeEmail, cancellationToken);
        
        return payslips.Select(MapToDto).OrderByDescending(p => p.Year).ThenByDescending(p => p.Month);
    }

    public async Task<PayslipDto?> GetPayslipByIdAsync(string payslipId, string employeeEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payslipId))
        {
            throw new ArgumentException("Payslip ID cannot be null or empty.", nameof(payslipId));
        }

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new ArgumentException("Employee email cannot be null or empty.", nameof(employeeEmail));
        }

        _logger.LogInformation("Retrieving payslip {PayslipId} for employee: {EmployeeEmail}", payslipId, employeeEmail);

        var payslip = await _repository.GetPayslipAsync(payslipId, cancellationToken);

        if (payslip == null)
        {
            _logger.LogWarning("Payslip {PayslipId} not found", payslipId);
            return null;
        }

        if (!payslip.BelongsTo(employeeEmail))
        {
            _logger.LogWarning("Unauthorized access attempt: Employee {EmployeeEmail} tried to access payslip {PayslipId} belonging to {Owner}",
                employeeEmail, payslipId, payslip.EmployeeEmail);
            return null;
        }

        await _repository.UpdateLastAccessedAsync(payslip.ContainerName, payslip.BlobName, cancellationToken);

        return MapToDto(payslip);
    }

    public async Task<Stream?> DownloadPayslipAsync(string payslipId, string employeeEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payslipId))
        {
            throw new ArgumentException("Payslip ID cannot be null or empty.", nameof(payslipId));
        }

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new ArgumentException("Employee email cannot be null or empty.", nameof(employeeEmail));
        }

        _logger.LogInformation("Downloading payslip {PayslipId} for employee: {EmployeeEmail}", payslipId, employeeEmail);

        var payslip = await _repository.GetPayslipAsync(payslipId, cancellationToken);

        if (payslip == null)
        {
            _logger.LogWarning("Payslip {PayslipId} not found", payslipId);
            return null;
        }

        if (!payslip.BelongsTo(employeeEmail))
        {
            _logger.LogWarning("Unauthorized download attempt: Employee {EmployeeEmail} tried to download payslip {PayslipId}",
                employeeEmail, payslipId);
            return null;
        }

        await _repository.UpdateLastAccessedAsync(payslip.ContainerName, payslip.BlobName, cancellationToken);

        return await _repository.DownloadPayslipContentAsync(payslip.ContainerName, payslip.BlobName, cancellationToken);
    }

    public async Task<string?> GetPayslipDownloadUrlAsync(string payslipId, string employeeEmail, TimeSpan validity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payslipId))
        {
            throw new ArgumentException("Payslip ID cannot be null or empty.", nameof(payslipId));
        }

        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            throw new ArgumentException("Employee email cannot be null or empty.", nameof(employeeEmail));
        }

        _logger.LogInformation("Generating download URL for payslip {PayslipId} for employee: {EmployeeEmail}", payslipId, employeeEmail);

        var payslip = await _repository.GetPayslipAsync(payslipId, cancellationToken);

        if (payslip == null)
        {
            _logger.LogWarning("Payslip {PayslipId} not found", payslipId);
            return null;
        }

        if (!payslip.BelongsTo(employeeEmail))
        {
            _logger.LogWarning("Unauthorized URL generation attempt: Employee {EmployeeEmail} tried to get URL for payslip {PayslipId}",
                employeeEmail, payslipId);
            return null;
        }

        return await _repository.GenerateSasUrlAsync(payslip.ContainerName, payslip.BlobName, validity, cancellationToken);
    }

    private static PayslipDto MapToDto(Payslip payslip)
    {
        return new PayslipDto
        {
            Id = payslip.Id,
            EmployeeId = payslip.EmployeeId,
            EmployeeEmail = payslip.EmployeeEmail,
            EmployeeName = payslip.EmployeeName,
            Year = payslip.Year,
            Month = payslip.Month,
            Period = payslip.GetPeriodDisplay(),
            FileSizeBytes = payslip.FileSizeBytes,
            FileSizeFormatted = FormatFileSize(payslip.FileSizeBytes),
            UploadedAt = payslip.UploadedAt,
            LastAccessedAt = payslip.LastAccessedAt,
            AccessTier = payslip.AccessTier,
            Status = payslip.Status
        };
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
