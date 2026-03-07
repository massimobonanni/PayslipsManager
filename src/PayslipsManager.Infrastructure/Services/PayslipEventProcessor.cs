using Microsoft.Extensions.Logging;
using PayslipsManager.Application.Interfaces;
using PayslipsManager.Domain.Entities;
using PayslipsManager.Infrastructure.Configuration;
using System.Globalization;

namespace PayslipsManager.Infrastructure.Services;

/// <summary>
/// Processes blob storage events for newly uploaded payslips.
/// Validates the blob name and extension, checks for idempotency, and applies blob index tags.
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
        string employeeId, string blobName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing payslip event -- Employee: {EmployeeId}, Blob: {BlobName}",
            employeeId, blobName);

        // ── Validation 1: File extension must be .pdf ────────────────
        if (!blobName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Validation failed -- '{BlobName}' does not have a .pdf extension.", blobName);
            return PayslipValidationResult.Failure(
                $"File '{blobName}' does not have a .pdf extension.");
        }

        // ── Validation 2: Blob name must match yyyy-MM.pdf ───────────
        var nameValidation = _storage.ValidateBlobName(blobName);
        if (!nameValidation.IsValid)
        {
            _logger.LogWarning(
                "Validation failed -- Blob name '{BlobName}': {Errors}",
                blobName, string.Join("; ", nameValidation.Errors));
            return nameValidation;
        }

        // ── Idempotency: Skip if tags are already applied ────────────
        var existing = await _storage.GetPayslipAsync(employeeId, blobName, cancellationToken);
        if (existing?.Tags.ContainsKey(PayslipTagKeys.DocumentType) == true)
        {
            _logger.LogInformation(
                "Idempotent skip -- Tags already present on '{BlobName}' for employee {EmployeeId}.",
                blobName, employeeId);
            return PayslipValidationResult.Success();
        }

        // ── Parse year and month from blob name ──────────────────────
        var (year, month) = ParseBlobName(blobName);

        // ── Apply blob index tags ────────────────────────────────────
        var tags = new Dictionary<string, string>
        {
            [PayslipTagKeys.EmployeeId] = employeeId,
            [PayslipTagKeys.PayslipYear] = year.ToString(CultureInfo.InvariantCulture),
            [PayslipTagKeys.PayslipMonth] = month.ToString("D2", CultureInfo.InvariantCulture),
            [PayslipTagKeys.DocumentType] = PayslipTagKeys.DocumentTypeValue
        };

        await _storage.SetTagsAsync(employeeId, blobName, tags, cancellationToken);

        _logger.LogInformation(
            "Tags applied -- Employee: {EmployeeId}, Blob: {BlobName}, Period: {Year}-{Month}",
            employeeId, blobName, year, month.ToString("D2", CultureInfo.InvariantCulture));

        return PayslipValidationResult.Success();
    }

    /// <summary>
    /// Extracts year and month from a blob name like "2026-03.pdf".
    /// </summary>
    private static (int Year, int Month) ParseBlobName(string blobName)
    {
        var name = Path.GetFileNameWithoutExtension(blobName);
        var parts = name.Split('-');
        var year = int.Parse(parts[0], CultureInfo.InvariantCulture);
        var month = int.Parse(parts[1], CultureInfo.InvariantCulture);
        return (year, month);
    }
}
