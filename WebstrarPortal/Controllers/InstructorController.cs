using Microsoft.AspNetCore.Mvc;
using WebstrarPortal.Models;
using WebstrarPortal.Services;

namespace WebstrarPortal.Controllers;

[Route("instructor")]
public class InstructorController : Controller
{
    private readonly DynamoDbService _ddb;
    private readonly PageStatusService _pageStatus;
    private readonly IConfiguration _config;
    private readonly HashSet<int> _instructorSites;
    private readonly HashSet<string> _taAsurites;

    public InstructorController(DynamoDbService ddb, PageStatusService pageStatus, IConfiguration config)
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
    public async Task<IActionResult> Index([FromQuery] string asurite)
    {
        if (string.IsNullOrWhiteSpace(asurite))
            return RedirectToAction("Index", "Portal");

        asurite = asurite.Trim().ToLowerInvariant();
        var isTA = _taAsurites.Contains(asurite);

        if (!isTA)
        {
            var site = await _ddb.GetSiteForAsuriteAsync(asurite);
            if (site == null || !_instructorSites.Contains(site.Value))
                return RedirectToAction("Index", "Portal");
        }

        var allAssignments = await _ddb.GetAllSiteAssignmentsAsync();
        var serviceBase = _config["CAS:ServiceBaseUrl"]?.TrimEnd('/') ?? Request.Scheme + "://" + Request.Host;

        // Build site overviews (exclude instructor sites)
        var sites = new List<SiteOverview>();
        foreach (var kvp in allAssignments.OrderBy(k => k.Key))
        {
            if (_instructorSites.Contains(kvp.Key))
                continue;

            sites.Add(new SiteOverview
            {
                SiteNumber = kvp.Key,
                Asurites = kvp.Value.OrderBy(a => a).ToList(),
                Pages = _pageStatus.GetPageStatuses(kvp.Key)
            });
        }

        var model = new InstructorDashboardViewModel
        {
            Asurite = asurite,
            SiteNumber = 0,
            Sites = sites,
            ServiceBaseUrl = serviceBase
        };

        return View("Index", model);
    }

    [HttpGet("site/{siteNumber:int}")]
    public async Task<IActionResult> Site(int siteNumber, [FromQuery] string asurite)
    {
        if (!await VerifyInstructor(asurite))
            return RedirectToAction("Index", "Portal");

        var allAssignments = await _ddb.GetAllSiteAssignmentsAsync();
        var serviceBase = _config["CAS:ServiceBaseUrl"]?.TrimEnd('/') ?? Request.Scheme + "://" + Request.Host;

        var siteAsurites = allAssignments.ContainsKey(siteNumber)
            ? allAssignments[siteNumber].OrderBy(a => a).ToList()
            : new List<string>();

        var model = new SiteOverview
        {
            SiteNumber = siteNumber,
            Asurites = siteAsurites,
            Pages = _pageStatus.GetPageStatuses(siteNumber)
        };

        ViewBag.Asurite = asurite;
        ViewBag.ServiceBaseUrl = serviceBase;
        return View("Site", model);
    }

    [HttpGet("site/{siteNumber:int}/page/{pageName}/files")]
    public async Task<IActionResult> Files(int siteNumber, string pageName, [FromQuery] string asurite)
    {
        if (!await VerifyInstructor(asurite))
            return RedirectToAction("Index", "Portal");

        var allAssignments = await _ddb.GetAllSiteAssignmentsAsync();
        var serviceBase = _config["CAS:ServiceBaseUrl"]?.TrimEnd('/') ?? Request.Scheme + "://" + Request.Host;

        var model = new FileBrowserViewModel
        {
            SiteNumber = siteNumber,
            PageName = pageName,
            Asurites = allAssignments.ContainsKey(siteNumber)
                ? allAssignments[siteNumber].OrderBy(a => a).ToList()
                : new List<string>(),
            Files = _pageStatus.GetFileTree(siteNumber, pageName),
            SiteBaseUrl = $"{serviceBase}/sites/website{siteNumber}/{pageName}"
        };

        ViewBag.Asurite = asurite;
        return View("Files", model);
    }

    [HttpGet("site/{siteNumber:int}/page/{pageName}/view")]
    public async Task<IActionResult> ViewFile(int siteNumber, string pageName,
        [FromQuery] string path, [FromQuery] string asurite)
    {
        if (!await VerifyInstructor(asurite))
            return RedirectToAction("Index", "Portal");

        if (string.IsNullOrWhiteSpace(path))
            return RedirectToAction("Files", new { siteNumber, pageName, asurite });

        var content = _pageStatus.ReadFileContent(siteNumber, pageName, path);
        if (content == null)
            return NotFound("File not found or access denied.");

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
        return View("ViewFile", model);
    }

    private async Task<bool> VerifyInstructor(string? asurite)
    {
        if (string.IsNullOrWhiteSpace(asurite))
            return false;

        asurite = asurite.Trim().ToLowerInvariant();

        if (_taAsurites.Contains(asurite))
            return true;

        var site = await _ddb.GetSiteForAsuriteAsync(asurite);
        return site.HasValue && _instructorSites.Contains(site.Value);
    }
}
