using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayslipsManager.Web.Models;

namespace PayslipsManager.Web.Controllers;

public class HomeController : Controller
{
    private readonly IConfiguration _configuration;

    public HomeController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Development-only login endpoint for testing without Entra ID.
    /// Requires BypassAuthentication=true in configuration.
    /// Example: /Home/DevLogin?email=alice.rossi@contoso.com
    /// </summary>
    public async Task<IActionResult> DevLogin(string email = "john.doe@company.com")
    {
        var bypassAuth = _configuration.GetValue<bool>("BypassAuthentication");
        if (!bypassAuth)
        {
            return NotFound();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, email.Split('@')[0]),
            new Claim(ClaimTypes.Email, email),
            new Claim("preferred_username", email),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        await HttpContext.SignInAsync("Cookies", claimsPrincipal);

        return RedirectToAction("Index", "Payslips");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
