using Microsoft.AspNetCore.Http;
using PayslipsManager.Application.Interfaces;
using PayslipsManager.Domain.Entities;
using PayslipsManager.Domain.Enums;
using System.Security.Claims;

namespace PayslipsManager.Web.Services;

/// <summary>
/// Resolves the current signed-in employee from the ASP.NET Core authentication context.
/// Uses Microsoft Entra claims to populate the Employee entity.
/// </summary>
public class EmployeeContextService : IEmployeeContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmployeeContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public Employee? GetCurrentEmployee()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var objectId = user.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                       ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(objectId))
            return null;

        return new Employee
        {
            EmployeeId = objectId,
            DisplayName = user.Identity?.Name
                          ?? user.FindFirstValue("name")
                          ?? string.Empty,
            EntraObjectId = objectId,
            EmploymentStatus = EmploymentStatus.Active
        };
    }
}
