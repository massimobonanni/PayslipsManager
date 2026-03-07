using PayslipsManager.Domain.Entities;

namespace PayslipsManager.Application.Interfaces;

/// <summary>
/// Processes blob storage events (e.g. new payslip uploaded).
/// Used by the Azure Functions project.
/// </summary>
public interface IPayslipEventProcessor
{
    /// <summary>
    /// Handles a newly uploaded payslip blob.
    /// Validates the blob, sets metadata/tags, and returns the validation result.
    /// </summary>
    Task<PayslipValidationResult> ProcessNewPayslipAsync(string blobName, Stream content, CancellationToken cancellationToken = default);
}
