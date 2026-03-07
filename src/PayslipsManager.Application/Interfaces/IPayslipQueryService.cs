using PayslipsManager.Application.DTOs;

namespace PayslipsManager.Application.Interfaces;

/// <summary>
/// Queries payslip metadata for a specific employee.
/// </summary>
public interface IPayslipQueryService
{
    /// <summary>
    /// Lists all payslips for an employee, ordered by date descending.
    /// </summary>
    Task<IReadOnlyList<PayslipListItemDto>> GetPayslipsForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full details of a specific payslip.
    /// Returns null if not found or the payslip does not belong to the employee.
    /// </summary>
    Task<PayslipDetailsDto?> GetPayslipDetailsAsync(string employeeId, string blobName, CancellationToken cancellationToken = default);
}
