using PayslipsManager.Domain.Enums;

namespace PayslipsManager.Application.DTOs;

/// <summary>
/// DTO representing the currently signed-in employee.
/// </summary>
public class EmployeeInfoDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EntraObjectId { get; set; } = string.Empty;
    public EmploymentStatus EmploymentStatus { get; set; }
}
