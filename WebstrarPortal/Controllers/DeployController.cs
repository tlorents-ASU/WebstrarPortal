using Microsoft.AspNetCore.Mvc;
using WebstrarPortal.Services;

namespace WebstrarPortal.Controllers;

[Route("portal/deploy")]
public class DeployController : Controller
{
    private readonly DynamoDbService _ddb;
    private readonly ZipDeployService _zip;

    public DeployController(DynamoDbService ddb, ZipDeployService zip)
    {
        _ddb = ddb;
        _zip = zip;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(1024L * 1024L * 1024L)] // 1 GB
    [RequestFormLimits(MultipartBodyLengthLimit = 1024L * 1024L * 1024L)]
    public async Task<IActionResult> Upload(
        [FromQuery] string page,
        [FromForm] string asurite,
        [FromForm] IFormFile file)
    {
        if (string.IsNullOrWhiteSpace(asurite))
        {
            TempData["Error"] = "ASURITE is required.";
            return RedirectToAction("Index", "Portal");
        }

        asurite = asurite.Trim().ToLowerInvariant();

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a ZIP file to upload.";
            return RedirectToAction("Index", "Portal", new { asurite });
        }

        try
        {
            page = NormalizeAndValidatePage(page);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "Portal", new { asurite });
        }

        var site = await _ddb.GetSiteForAsuriteAsync(asurite);
        if (site == null)
        {
            TempData["Error"] = "No site assignment found for your ASURITE.";
            return RedirectToAction("Index", "Portal", new { asurite });
        }

        var deployFolder = $@"C:\WebstrarDeploy\website{site}\{page}\";
        var deployRoot = @"C:\WebstrarDeploy\";

        var fullDeployFolder = Path.GetFullPath(deployFolder);
        var fullDeployRoot = Path.GetFullPath(deployRoot);

        if (!fullDeployFolder.StartsWith(fullDeployRoot, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Invalid deploy path (safety check failed).";
            return RedirectToAction("Index", "Portal", new { asurite });
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
            return RedirectToAction("Index", "Portal", new { asurite });
        }

        var tempZip = Path.Combine(Path.GetTempPath(),
            $"webstrar_{asurite}_{site}_{page}_{Guid.NewGuid():N}.zip");

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
            return RedirectToAction("Index", "Portal", new { asurite });
        }
        finally
        {
            try { System.IO.File.Delete(tempZip); } catch { }
        }

        TempData["Success"] = $"Deployed {extracted} file(s) to {page} successfully.";
        return RedirectToAction("Index", "Portal", new { asurite });
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
