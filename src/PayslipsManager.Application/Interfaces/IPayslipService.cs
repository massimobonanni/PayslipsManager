using PayslipsManager.Application.DTOs;

namespace PayslipsManager.Application.Interfaces;

/// <summary>
/// Service for managing employee payslips.
/// </summary>
public interface IPayslipService
{
    /// <summary>
    /// Gets all payslips for a specific employee.
    /// </summary>
    Task<IEnumerable<PayslipDto>> GetPayslipsForEmployeeAsync(string employeeEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific payslip by ID for an employee.
    /// </summary>
    Task<PayslipDto?> GetPayslipByIdAsync(string payslipId, string employeeEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a payslip as a stream.
    /// </summary>
    Task<Stream?> DownloadPayslipAsync(string payslipId, string employeeEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the download URL for a payslip with a SAS token.
    /// </summary>
    Task<string?> GetPayslipDownloadUrlAsync(string payslipId, string employeeEmail, TimeSpan validity, CancellationToken cancellationToken = default);
}
