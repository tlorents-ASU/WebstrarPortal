using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebstrarPortal.Models;
using WebstrarPortal.Services;

namespace WebstrarPortal.Controllers;

[Route("portal")]
[Authorize]
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
        _multiSiteUsers = _multiSiteUsers
            .ToDictionary(k => k.Key.ToLowerInvariant(), v => v.Value);
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] int? site)
    {
        var asurite = User.Identity!.Name!;

        // Multi-site users: show site picker or dashboard for selected site
        if (_multiSiteUsers.TryGetValue(asurite, out var sites))
        {
            if (site.HasValue && sites.Contains(site.Value))
            {
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

            // Check if this multi-site user is also an instructor or TA
            var isInstructor = false;
            var ddbSite = await _ddb.GetSiteForAsuriteAsync(asurite);
            if (ddbSite.HasValue && _instructorSites.Contains(ddbSite.Value))
                isInstructor = true;
            if (_taAsurites.Contains(asurite))
                isInstructor = true;

            ViewBag.Asurite = asurite;
            ViewBag.Sites = sites;
            ViewBag.ShowInstructorDashboard = isInstructor;
            return View("SitePicker");
        }

        // TAs go straight to instructor dashboard
        if (_taAsurites.Contains(asurite))
        {
            return RedirectToAction("Index", "Instructor");
        }

        var siteResult = await _ddb.GetSiteForAsuriteAsync(asurite);

        if (siteResult == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Instructor sites get redirected to instructor dashboard
        if (_instructorSites.Contains(siteResult.Value))
        {
            return RedirectToAction("Index", "Instructor");
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
    public async Task<IActionResult> Files(int siteNumber, string pageName)
    {
        var asurite = User.Identity!.Name!;

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

        // For multi-site users, back URL includes site param
        if (_multiSiteUsers.ContainsKey(asurite))
            ViewBag.BackUrl = $"/portal?site={siteNumber}";
        else
            ViewBag.BackUrl = "/portal";

        return View("Files", model);
    }

    [HttpGet("files/{siteNumber:int}/{pageName}/view")]
    public async Task<IActionResult> ViewFile(int siteNumber, string pageName,
        [FromQuery] string path)
    {
        var asurite = User.Identity!.Name!;

        if (!await UserOwnsSite(asurite, siteNumber))
            return RedirectToAction("Index");

        if (string.IsNullOrWhiteSpace(path))
            return RedirectToAction("Files", new { siteNumber, pageName });

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

        if (_multiSiteUsers.ContainsKey(asurite))
            ViewBag.BackUrl = $"/portal?site={siteNumber}";
        else
            ViewBag.BackUrl = "/portal";

        return View("ViewFile", model);
    }

    private async Task<bool> UserOwnsSite(string asurite, int siteNumber)
    {
        if (_multiSiteUsers.TryGetValue(asurite, out var sites) && sites.Contains(siteNumber))
            return true;

        var site = await _ddb.GetSiteForAsuriteAsync(asurite);
        return site.HasValue && site.Value == siteNumber;
    }
}
