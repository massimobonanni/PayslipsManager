using PayslipsManager.Domain.Enums;

namespace PayslipsManager.Domain.Entities;

/// <summary>
/// Represents an employee who can access their own payslips.
/// </summary>
public class Employee
{
    public string EmployeeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EntraObjectId { get; set; } = string.Empty;
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

    /// <summary>
    /// Checks whether this employee is currently active.
    /// </summary>
    public bool IsActive => EmploymentStatus == EmploymentStatus.Active;
}
