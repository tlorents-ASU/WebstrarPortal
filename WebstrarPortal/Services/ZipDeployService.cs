using System.IO.Compression;

namespace WebstrarPortal.Services;

public class ZipDeployService
{
    public int ExtractZipToFolder(string zipPath, string destinationFolder)
    {
        Directory.CreateDirectory(destinationFolder);

        int extractedCount = 0;

        using var archive = ZipFile.OpenRead(zipPath);

        foreach (var entry in archive.Entries)
        {
            // Skip directory entries
            if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith("/"))
                continue;

            // Normalize + prevent Zip Slip (path traversal)
            var destPath = Path.GetFullPath(Path.Combine(destinationFolder, entry.FullName));

            if (!destPath.StartsWith(Path.GetFullPath(destinationFolder), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Blocked zip-slip path traversal: " + entry.FullName);

            // Ensure folder exists
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrWhiteSpace(destDir))
                Directory.CreateDirectory(destDir);

            // Overwrite if exists
            entry.ExtractToFile(destPath, overwrite: true);
            extractedCount++;
        }

        return extractedCount;
    }
}
