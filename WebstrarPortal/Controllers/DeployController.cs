using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebstrarPortal.Services;

namespace WebstrarPortal.Controllers;

[Route("portal/deploy")]
[Authorize]
public class DeployController : Controller
{
    private readonly DynamoDbService _ddb;
    private readonly ZipDeployService _zip;
    private readonly IConfiguration _config;
    private readonly Dictionary<string, List<int>> _multiSiteUsers;

    public DeployController(DynamoDbService ddb, ZipDeployService zip, IConfiguration config)
    {
        _ddb = ddb;
        _zip = zip;
        _config = config;
        _multiSiteUsers = config.GetSection("MultiSiteUsers")
            .Get<Dictionary<string, List<int>>>() ?? new Dictionary<string, List<int>>();
        _multiSiteUsers = _multiSiteUsers
            .ToDictionary(k => k.Key.ToLowerInvariant(), v => v.Value);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(1024L * 1024L * 1024L)] // 1 GB
    [RequestFormLimits(MultipartBodyLengthLimit = 1024L * 1024L * 1024L)]
    public async Task<IActionResult> Upload(
        [FromQuery] string page,
        [FromForm] IFormFile file,
        [FromForm] int? siteOverride)
    {
        var asurite = User.Identity!.Name!;

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a ZIP file to upload.";
            return RedirectToPortal(asurite, siteOverride);
        }

        try
        {
            page = NormalizeAndValidatePage(page);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPortal(asurite, siteOverride);
        }

        // Resolve site number: multi-site override or DynamoDB
        int siteNumber;
        if (siteOverride.HasValue
            && _multiSiteUsers.TryGetValue(asurite, out var allowedSites)
            && allowedSites.Contains(siteOverride.Value))
        {
            siteNumber = siteOverride.Value;
        }
        else
        {
            var site = await _ddb.GetSiteForAsuriteAsync(asurite);
            if (site == null)
            {
                TempData["Error"] = "No site assignment found for your ASURITE.";
                return RedirectToAction("Index", "Portal");
            }
            siteNumber = site.Value;
        }

        var deployFolder = $@"C:\WebstrarDeploy\website{siteNumber}\{page}\";
        var deployRoot = @"C:\WebstrarDeploy\";

        var fullDeployFolder = Path.GetFullPath(deployFolder);
        var fullDeployRoot = Path.GetFullPath(deployRoot);

        if (!fullDeployFolder.StartsWith(fullDeployRoot, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Invalid deploy path (safety check failed).";
            return RedirectToPortal(asurite, siteOverride);
        }

        try
        {
            if (Directory.Exists(fullDeployFolder))
                Directory.Delete(fullDeployFolder, recursive: true);

            Directory.CreateDirectory(fullDeployFolder);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to prepare deploy folder: {ex.Message}";
            return RedirectToPortal(asurite, siteOverride);
        }

        var tempZip = Path.Combine(Path.GetTempPath(),
            $"webstrar_{asurite}_{siteNumber}_{page}_{Guid.NewGuid():N}.zip");

        await using (var fs = System.IO.File.Create(tempZip))
        {
            await file.CopyToAsync(fs);
        }

        int extracted;
        try
        {
            extracted = _zip.ExtractZipToFolder(tempZip, fullDeployFolder);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to extract ZIP: {ex.Message}";
            return RedirectToPortal(asurite, siteOverride);
        }
        finally
        {
            try { System.IO.File.Delete(tempZip); } catch { }
        }

        TempData["Success"] = $"Deployed {extracted} file(s) to {page} successfully.";
        return RedirectToPortal(asurite, siteOverride);
    }

    [HttpPost("clear")]
    public async Task<IActionResult> Clear(
        [FromQuery] string page,
        [FromForm] int? siteOverride)
    {
        var asurite = User.Identity!.Name!;

        try
        {
            page = NormalizeAndValidatePage(page);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPortal(asurite, siteOverride);
        }

        // Resolve site number: multi-site override or DynamoDB
        int siteNumber;
        if (siteOverride.HasValue
            && _multiSiteUsers.TryGetValue(asurite, out var allowedSites)
            && allowedSites.Contains(siteOverride.Value))
        {
            siteNumber = siteOverride.Value;
        }
        else
        {
            var site = await _ddb.GetSiteForAsuriteAsync(asurite);
            if (site == null)
            {
                TempData["Error"] = "No site assignment found for your ASURITE.";
                return RedirectToAction("Index", "Portal");
            }
            siteNumber = site.Value;
        }

        var deployFolder = $@"C:\WebstrarDeploy\website{siteNumber}\{page}\";
        var deployRoot = @"C:\WebstrarDeploy\";

        var fullDeployFolder = Path.GetFullPath(deployFolder);
        var fullDeployRoot = Path.GetFullPath(deployRoot);

        if (!fullDeployFolder.StartsWith(fullDeployRoot, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Invalid deploy path (safety check failed).";
            return RedirectToPortal(asurite, siteOverride);
        }

        try
        {
            if (Directory.Exists(fullDeployFolder))
                Directory.Delete(fullDeployFolder, recursive: true);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to remove files: {ex.Message}";
            return RedirectToPortal(asurite, siteOverride);
        }

        TempData["Success"] = $"All files removed from {page}.";
        return RedirectToPortal(asurite, siteOverride);
    }

    private IActionResult RedirectToPortal(string asurite, int? siteOverride)
    {
        // Multi-site users need the site param to return to the right dashboard
        if (siteOverride.HasValue && _multiSiteUsers.ContainsKey(asurite))
            return RedirectToAction("Index", "Portal", new { site = siteOverride.Value });
        return RedirectToAction("Index", "Portal");
    }

    private static string NormalizeAndValidatePage(string? page)
    {
        if (string.IsNullOrWhiteSpace(page))
            throw new ArgumentException("Missing page");

        page = page.Trim();

        var allowed = Enumerable.Range(0, 11).Select(i => $"Page{i}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!allowed.Contains(page))
            throw new ArgumentException("Invalid page. Must be Page0..Page10");

        return allowed.First(p => p.Equals(page, StringComparison.OrdinalIgnoreCase));
    }
}
