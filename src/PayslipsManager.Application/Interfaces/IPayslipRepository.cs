using PayslipsManager.Domain.Entities;

namespace PayslipsManager.Application.Interfaces;

/// <summary>
/// Repository interface for payslip blob storage operations.
/// </summary>
public interface IPayslipRepository
{
    /// <summary>
    /// Lists all payslips for a specific employee.
    /// </summary>
    Task<IEnumerable<Payslip>> ListPayslipsAsync(string employeeEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payslip by its ID.
    /// </summary>
    Task<Payslip?> GetPayslipAsync(string payslipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads payslip content as a stream.
    /// </summary>
    Task<Stream?> DownloadPayslipContentAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a SAS URL for direct download.
    /// </summary>
    Task<string?> GenerateSasUrlAsync(string containerName, string blobName, TimeSpan validity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last accessed timestamp for a payslip.
    /// </summary>
    Task UpdateLastAccessedAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
}
