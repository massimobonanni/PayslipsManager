using Microsoft.Extensions.Logging;
using PayslipsManager.Application.Interfaces;
using PayslipsManager.Domain.Entities;
using PayslipsManager.Infrastructure.Configuration;
using System.Globalization;

namespace PayslipsManager.Infrastructure.Services;

/// <summary>
/// Processes blob storage events for newly uploaded payslips.
/// Validates the blob and sets metadata and blob index tags.
/// </summary>
public class PayslipEventProcessor : IPayslipEventProcessor
{
    private readonly IPayslipStorageService _storage;
    private readonly ILogger<PayslipEventProcessor> _logger;

    public PayslipEventProcessor(
        IPayslipStorageService storage,
        ILogger<PayslipEventProcessor> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PayslipValidationResult> ProcessNewPayslipAsync(
        string employeeId, string blobName, Stream content,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing new payslip {BlobName} for employee {EmployeeId}",
            blobName, employeeId);

        // Validate blob name format (yyyy-MM.pdf)
        var nameValidation = _storage.ValidateBlobName(blobName);
        if (!nameValidation.IsValid)
        {
            _logger.LogWarning("Blob name validation failed for {BlobName}: {Errors}",
                blobName, string.Join("; ", nameValidation.Errors));
            return nameValidation;
        }

        // Validate content type
        var contentValidation = _storage.ValidateContentType("application/pdf");
        if (!contentValidation.IsValid)
        {
            _logger.LogWarning("Content type validation failed for {BlobName}: {Errors}",
                blobName, string.Join("; ", contentValidation.Errors));
            return contentValidation;
        }

        // Parse year and month from blob name
        var (year, month) = ParseBlobName(blobName);

        // Build tags
        var tags = new Dictionary<string, string>
        {
            [PayslipTagKeys.EmployeeId] = employeeId,
            [PayslipTagKeys.PayslipYear] = year.ToString(CultureInfo.InvariantCulture),
            [PayslipTagKeys.PayslipMonth] = month.ToString("D2", CultureInfo.InvariantCulture),
            [PayslipTagKeys.DocumentType] = PayslipTagKeys.DocumentTypeValue
        };

        await _storage.SetTagsAsync(employeeId, blobName, tags, cancellationToken);

        _logger.LogInformation("Successfully processed payslip {BlobName} for employee {EmployeeId}",
            blobName, employeeId);

        return PayslipValidationResult.Success();
    }

    private static (int Year, int Month) ParseBlobName(string blobName)
    {
        var name = Path.GetFileNameWithoutExtension(blobName);
        var parts = name.Split('-');
        var year = int.Parse(parts[0], CultureInfo.InvariantCulture);
        var month = int.Parse(parts[1], CultureInfo.InvariantCulture);
        return (year, month);
    }
}
