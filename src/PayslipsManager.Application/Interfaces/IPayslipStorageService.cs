using PayslipsManager.Domain.Entities;

namespace PayslipsManager.Application.Interfaces;

/// <summary>
/// Low-level storage operations for payslip blobs.
/// Implemented by the infrastructure layer against Azure Blob Storage.
/// </summary>
public interface IPayslipStorageService
{
    /// <summary>
    /// Lists all payslip documents in the employee's container.
    /// </summary>
    Task<IReadOnlyList<PayslipDocument>> ListPayslipsAsync(string employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single payslip document by blob name from the employee's container.
    /// </summary>
    Task<PayslipDocument?> GetPayslipAsync(string employeeId, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads payslip content as a stream.
    /// </summary>
    Task<Stream?> DownloadContentAsync(string employeeId, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a time-limited SAS URL for direct download.
    /// </summary>
    Task<string?> GenerateSasUrlAsync(string employeeId, string blobName, TimeSpan validity, CancellationToken cancellationToken = default);
}
