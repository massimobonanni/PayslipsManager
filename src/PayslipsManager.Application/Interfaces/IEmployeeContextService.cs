using PayslipsManager.Domain.Entities;

namespace PayslipsManager.Application.Interfaces;

/// <summary>
/// Resolves the current signed-in employee from the authentication context.
/// </summary>
public interface IEmployeeContextService
{
    /// <summary>
    /// Gets the employee information for the currently signed-in user.
    /// Returns null if the user is not authenticated.
    /// </summary>
    Employee? GetCurrentEmployee();
}
