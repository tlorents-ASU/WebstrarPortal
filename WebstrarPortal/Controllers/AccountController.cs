using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using WebstrarPortal.Services;

namespace WebstrarPortal.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly CasTicketValidator _cas;
    private readonly IConfiguration _config;
    private readonly ILogger<AccountController> _log;

    public AccountController(CasTicketValidator cas, IConfiguration config, ILogger<AccountController> log)
    {
        _cas = cas;
        _config = config;
        _log = log;
    }

    /// <summary>
    /// Redirects to CAS login page.
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        var callbackUrl = BuildCallbackUrl(returnUrl);
        var casLoginUrl = _cas.BuildLoginUrl(callbackUrl);
        return Redirect(casLoginUrl);
    }

    /// <summary>
    /// CAS redirects back here with a ticket after successful login.
    /// </summary>
    [HttpGet("cas-callback")]
    public async Task<IActionResult> CasCallback(
        [FromQuery] string ticket,
        [FromQuery] string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(ticket))
            return BadRequest("Missing CAS ticket");

        var callbackUrl = BuildCallbackUrl(returnUrl);
        var asurite = await _cas.ValidateAsync(ticket, callbackUrl);

        if (string.IsNullOrWhiteSpace(asurite))
        {
            _log.LogWarning("CAS ticket validation failed for ticket: {Ticket}", ticket);
            return RedirectToAction("Login");
        }

        _log.LogInformation("CAS login successful for {Asurite}", asurite);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, asurite),
            new(ClaimTypes.NameIdentifier, asurite)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        return Redirect(returnUrl ?? "/portal");
    }

    /// <summary>
    /// Signs out locally and redirects to CAS logout.
    /// </summary>
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var serviceBase = _config["CAS:ServiceBaseUrl"] ?? "";
        var casLogoutUrl = _cas.BuildLogoutUrl(serviceBase);
        return Redirect(casLogoutUrl);
    }

    /// <summary>
    /// Shown when an authenticated user has no site assignment.
    /// </summary>
    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private string BuildCallbackUrl(string? returnUrl)
    {
        var serviceBase = _config["CAS:ServiceBaseUrl"]?.TrimEnd('/') ?? "";
        var callback = $"{serviceBase}/account/cas-callback";
        if (!string.IsNullOrWhiteSpace(returnUrl))
            callback += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
        return callback;
    }
}
