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
    private readonly HashSet<int> _instructorSites;
    private readonly HashSet<string> _taAsurites;

    public PortalController(DynamoDbService ddb, PageStatusService pageStatus, IConfiguration config)
    {
        _ddb = ddb;
        _pageStatus = pageStatus;
        _config = config;
        _instructorSites = config.GetSection("InstructorSites")
            .Get<int[]>()?.ToHashSet() ?? new HashSet<int> { 1, 99, 100 };
        _taAsurites = config.GetSection("TaAsurites")
            .Get<string[]>()?.Select(a => a.ToLowerInvariant()).ToHashSet()
            ?? new HashSet<string>();
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] string? asurite)
    {
        if (string.IsNullOrWhiteSpace(asurite))
        {
            return View("Index", new DashboardViewModel
            {
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string
            });
        }

        asurite = asurite.Trim().ToLowerInvariant();

        // TAs go straight to instructor dashboard (no DynamoDB entry needed)
        if (_taAsurites.Contains(asurite))
        {
            return RedirectToAction("Index", "Instructor", new { asurite });
        }

        var site = await _ddb.GetSiteForAsuriteAsync(asurite);

        if (site == null)
        {
            return View("Index", new DashboardViewModel
            {
                ErrorMessage = $"No site assignment found for ASURITE \"{asurite}\". Contact your instructor."
            });
        }

        // Instructor sites get redirected to instructor dashboard
        if (_instructorSites.Contains(site.Value))
        {
            return RedirectToAction("Index", "Instructor", new { asurite });
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
