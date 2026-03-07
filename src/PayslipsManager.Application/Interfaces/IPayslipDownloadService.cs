namespace PayslipsManager.Application.Interfaces;

/// <summary>
/// Downloads payslip content from blob storage.
/// </summary>
public interface IPayslipDownloadService
{
    /// <summary>
    /// Downloads the payslip PDF as a stream.
    /// Returns null if the payslip is not found or does not belong to the employee.
    /// </summary>
    Task<Stream?> DownloadAsync(string employeeId, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a short-lived SAS URL for direct browser download.
    /// Returns null if the payslip is not found or does not belong to the employee.
    /// </summary>
    Task<string?> GenerateDownloadUrlAsync(string employeeId, string blobName, TimeSpan validity, CancellationToken cancellationToken = default);
}
