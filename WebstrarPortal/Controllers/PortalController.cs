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
    private readonly Dictionary<string, List<int>> _multiSiteUsers;

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
        _multiSiteUsers = config.GetSection("MultiSiteUsers")
            .Get<Dictionary<string, List<int>>>() ?? new Dictionary<string, List<int>>();
        // Normalize keys to lowercase
        _multiSiteUsers = _multiSiteUsers
            .ToDictionary(k => k.Key.ToLowerInvariant(), v => v.Value);
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] string? asurite, [FromQuery] int? site)
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

        // Multi-site users: show site picker or dashboard for selected site
        if (_multiSiteUsers.TryGetValue(asurite, out var sites))
        {
            if (site.HasValue && sites.Contains(site.Value))
            {
                // They picked a site — show the student dashboard for it
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

            // No site selected — show the site picker
            ViewBag.Asurite = asurite;
            ViewBag.Sites = sites;
            return View("SitePicker");
        }

        var siteResult = await _ddb.GetSiteForAsuriteAsync(asurite);

        if (siteResult == null)
        {
            return View("Index", new DashboardViewModel
            {
                ErrorMessage = $"No site assignment found for ASURITE \"{asurite}\". Contact your instructor."
            });
        }

        // Instructor sites get redirected to instructor dashboard
        if (_instructorSites.Contains(siteResult.Value))
        {
            return RedirectToAction("Index", "Instructor", new { asurite });
        }

        {
            var serviceBase = _config["CAS:ServiceBaseUrl"]?.TrimEnd('/') ?? Request.Scheme + "://" + Request.Host;

            var model = new DashboardViewModel
            {
                Asurite = asurite,
                SiteNumber = siteResult.Value,
                Pages = _pageStatus.GetPageStatuses(siteResult.Value),
                ServiceBaseUrl = serviceBase,
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string
            };

            return View("Index", model);
        }
    }

    [HttpGet("files/{siteNumber:int}/{pageName}")]
    public async Task<IActionResult> Files(int siteNumber, string pageName, [FromQuery] string asurite)
    {
        if (string.IsNullOrWhiteSpace(asurite))
            return RedirectToAction("Index");

        asurite = asurite.Trim().ToLowerInvariant();

        if (!await UserOwnsSite(asurite, siteNumber))
            return RedirectToAction("Index");

        var serviceBase = _config["CAS:ServiceBaseUrl"]?.TrimEnd('/') ?? Request.Scheme + "://" + Request.Host;

        var pageStatuses = _pageStatus.GetPageStatuses(siteNumber);
        var thisPage = pageStatuses.FirstOrDefault(p => p.Name == pageName);

        var model = new FileBrowserViewModel
        {
            SiteNumber = siteNumber,
            PageName = pageName,
            Files = _pageStatus.GetFileTree(siteNumber, pageName),
            SiteBaseUrl = $"{serviceBase}/sites/website{siteNumber}/{pageName}",
            AspxFiles = thisPage?.AspxFiles ?? new List<string>(),
            EntryFile = thisPage?.EntryFile
        };

        ViewBag.Asurite = asurite;
        ViewBag.BackUrl = $"/portal?asurite={asurite}&site={siteNumber}";
        return View("Files", model);
    }

    [HttpGet("files/{siteNumber:int}/{pageName}/view")]
    public async Task<IActionResult> ViewFile(int siteNumber, string pageName,
        [FromQuery] string path, [FromQuery] string asurite)
    {
        if (string.IsNullOrWhiteSpace(asurite))
            return RedirectToAction("Index");

        asurite = asurite.Trim().ToLowerInvariant();

        if (!await UserOwnsSite(asurite, siteNumber))
            return RedirectToAction("Index");

        if (string.IsNullOrWhiteSpace(path))
            return RedirectToAction("Files", new { siteNumber, pageName, asurite });

        var content = _pageStatus.ReadFileContent(siteNumber, pageName, path);
        if (content == null)
            return NotFound("File not found.");

        var model = new CodeViewerViewModel
        {
            SiteNumber = siteNumber,
            PageName = pageName,
            FilePath = path,
            FileName = Path.GetFileName(path),
            Content = content,
            Language = PageStatusService.GetLanguageFromExtension(path)
        };

        ViewBag.Asurite = asurite;
        ViewBag.BackUrl = $"/portal?asurite={asurite}&site={siteNumber}";
        return View("ViewFile", model);
    }

    /// <summary>
    /// Checks if a user owns a site — either via DynamoDB or MultiSiteUsers config.
    /// </summary>
    private async Task<bool> UserOwnsSite(string asurite, int siteNumber)
    {
        // Check multi-site config first
        if (_multiSiteUsers.TryGetValue(asurite, out var sites) && sites.Contains(siteNumber))
            return true;

        // Fall back to DynamoDB
        var site = await _ddb.GetSiteForAsuriteAsync(asurite);
        return site.HasValue && site.Value == siteNumber;
    }
}
