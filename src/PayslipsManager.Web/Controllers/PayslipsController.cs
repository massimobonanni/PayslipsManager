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
    private readonly IPayslipService _payslipService;
    private readonly ILogger<PayslipsController> _logger;

    public PayslipsController(IPayslipService payslipService, ILogger<PayslipsController> logger)
    {
        _payslipService = payslipService ?? throw new ArgumentNullException(nameof(payslipService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the list of payslips for the authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmail();
        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("User email not found in claims");
            return Unauthorized();
        }

        _logger.LogInformation("Fetching payslips for user: {UserEmail}", userEmail);

        var payslips = await _payslipService.GetPayslipsForEmployeeAsync(userEmail, cancellationToken);
        
        ViewData["UserEmail"] = userEmail;
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
            return BadRequest("Payslip ID is required.");
        }

        var userEmail = GetUserEmail();
        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("User email not found in claims");
            return Unauthorized();
        }

        _logger.LogInformation("User {UserEmail} downloading payslip: {PayslipId}", userEmail, id);

        var stream = await _payslipService.DownloadPayslipAsync(id, userEmail, cancellationToken);

        if (stream == null)
        {
            _logger.LogWarning("Payslip {PayslipId} not found or unauthorized access by {UserEmail}", id, userEmail);
            return NotFound();
        }

        return File(stream, "application/pdf", $"payslip_{id}.pdf");
    }

    /// <summary>
    /// Displays details for a specific payslip.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Payslip ID is required.");
        }

        var userEmail = GetUserEmail();
        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("User email not found in claims");
            return Unauthorized();
        }

        _logger.LogInformation("User {UserEmail} viewing details for payslip: {PayslipId}", userEmail, id);

        var payslip = await _payslipService.GetPayslipByIdAsync(id, userEmail, cancellationToken);

        if (payslip == null)
        {
            _logger.LogWarning("Payslip {PayslipId} not found or unauthorized access by {UserEmail}", id, userEmail);
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
            return BadRequest("Payslip ID is required.");
        }

        var userEmail = GetUserEmail();
        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("User email not found in claims");
            return Unauthorized();
        }

        _logger.LogInformation("User {UserEmail} requesting download URL for payslip: {PayslipId}", userEmail, id);

        var downloadUrl = await _payslipService.GetPayslipDownloadUrlAsync(id, userEmail, TimeSpan.FromMinutes(15), cancellationToken);

        if (downloadUrl == null)
        {
            _logger.LogWarning("Payslip {PayslipId} not found or unauthorized access by {UserEmail}", id, userEmail);
            return NotFound();
        }

        return Json(new { downloadUrl });
    }

    private string? GetUserEmail()
    {
        // Try to get email from preferred_username claim (Microsoft Entra ID)
        var email = User.FindFirstValue("preferred_username") 
                    ?? User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue("email");

        return email;
    }
}
