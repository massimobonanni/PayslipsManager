using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayslipsManager.Application.Interfaces;
using System.Security.Claims;

namespace PayslipsManager.Web.Controllers;

/// <summary>
/// Controller for managing employee payslips.
/// </summary>
[Authorize]
public class PayslipsController : Controller
{
    private readonly IPayslipQueryService _queryService;
    private readonly IPayslipDownloadService _downloadService;
    private readonly ILogger<PayslipsController> _logger;

    public PayslipsController(
        IPayslipQueryService queryService,
        IPayslipDownloadService downloadService,
        ILogger<PayslipsController> logger)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the list of payslips for the authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var employeeId = GetEmployeeId();
        if (string.IsNullOrEmpty(employeeId))
        {
            _logger.LogWarning("Employee identifier not found in claims");
            return Unauthorized();
        }

        _logger.LogInformation("Fetching payslips for employee {EmployeeId}", employeeId);

        var payslips = await _queryService.GetPayslipsForEmployeeAsync(employeeId, cancellationToken);

        ViewData["UserEmail"] = User.FindFirstValue("preferred_username") ?? User.FindFirstValue(ClaimTypes.Email);
        ViewData["UserName"] = User.Identity?.Name ?? "User";

        return View(payslips);
    }

    /// <summary>
    /// Downloads a payslip PDF file.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Download(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Payslip blob name is required.");
        }

        var employeeId = GetEmployeeId();
        if (string.IsNullOrEmpty(employeeId))
        {
            _logger.LogWarning("Employee identifier not found in claims");
            return Unauthorized();
        }

        _logger.LogInformation("Employee {EmployeeId} downloading payslip {BlobName}", employeeId, id);

        var stream = await _downloadService.DownloadAsync(employeeId, id, cancellationToken);

        if (stream == null)
        {
            _logger.LogWarning("Payslip {BlobName} not found or unauthorized for employee {EmployeeId}", id, employeeId);
            return NotFound();
        }

        return File(stream, "application/pdf", $"payslip_{id}");
    }

    /// <summary>
    /// Displays details for a specific payslip.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Payslip blob name is required.");
        }

        var employeeId = GetEmployeeId();
        if (string.IsNullOrEmpty(employeeId))
        {
            _logger.LogWarning("Employee identifier not found in claims");
            return Unauthorized();
        }

        _logger.LogInformation("Employee {EmployeeId} viewing details for payslip {BlobName}", employeeId, id);

        var payslip = await _queryService.GetPayslipDetailsAsync(employeeId, id, cancellationToken);

        if (payslip == null)
        {
            _logger.LogWarning("Payslip {BlobName} not found or unauthorized for employee {EmployeeId}", id, employeeId);
            return NotFound();
        }

        return View(payslip);
    }

    /// <summary>
    /// Gets a temporary download URL with SAS token.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDownloadUrl(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Payslip blob name is required.");
        }

        var employeeId = GetEmployeeId();
        if (string.IsNullOrEmpty(employeeId))
        {
            _logger.LogWarning("Employee identifier not found in claims");
            return Unauthorized();
        }

        _logger.LogInformation("Employee {EmployeeId} requesting download URL for payslip {BlobName}", employeeId, id);

        var downloadUrl = await _downloadService.GenerateDownloadUrlAsync(employeeId, id, TimeSpan.FromMinutes(15), cancellationToken);

        if (downloadUrl == null)
        {
            _logger.LogWarning("Payslip {BlobName} not found or unauthorized for employee {EmployeeId}", id, employeeId);
            return NotFound();
        }

        return Json(new { downloadUrl });
    }

    private string? GetEmployeeId()
    {
        // Use the Entra Object ID (oid claim) as the employee identifier
        return User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
