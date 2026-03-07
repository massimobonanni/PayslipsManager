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
    /// Validates the blob name and content type, then sets blob index tags.
    /// </summary>
    Task<PayslipValidationResult> ProcessNewPayslipAsync(string employeeId, string blobName, Stream content, CancellationToken cancellationToken = default);
}
