using Microsoft.AspNetCore.Mvc;
using WebstrarPortal.Models;
using WebstrarPortal.Services;

namespace WebstrarPortal.Controllers;

[Route("portal")]
public class PortalController : Controller
{
    private readonly DynamoDbService _ddb;
    private readonly PageStatusService _pageStatus;
    private readonly IConfiguration _config;

    public PortalController(DynamoDbService ddb, PageStatusService pageStatus, IConfiguration config)
    {
        _ddb = ddb;
        _pageStatus = pageStatus;
        _config = config;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] string? asurite)
    {
        // If no ASURITE provided, show the login/entry form
        if (string.IsNullOrWhiteSpace(asurite))
        {
            return View("Index", new DashboardViewModel
            {
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string
            });
        }

        asurite = asurite.Trim().ToLowerInvariant();
        var site = await _ddb.GetSiteForAsuriteAsync(asurite);

        if (site == null)
        {
            return View("Index", new DashboardViewModel
            {
                ErrorMessage = $"No site assignment found for ASURITE \"{asurite}\". Contact your instructor."
            });
        }

        var serviceBase = _config["CAS:ServiceBaseUrl"]?.TrimEnd('/') ?? Request.Scheme + "://" + Request.Host;

        var model = new DashboardViewModel
        {
            Asurite = asurite,
            SiteNumber = site.Value,
            Pages = _pageStatus.GetPageStatuses(site.Value),
            ServiceBaseUrl = serviceBase,
            SuccessMessage = TempData["Success"] as string,
            ErrorMessage = TempData["Error"] as string
        };

        return View("Index", model);
    }
}
