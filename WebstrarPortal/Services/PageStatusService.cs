using WebstrarPortal.Models;

namespace WebstrarPortal.Services;

public class PageStatusService
{
    private const string DeployRoot = @"C:\WebstrarDeploy";

    public List<PageSlot> GetPageStatuses(int siteNumber)
    {
        var pages = new List<PageSlot>();

        for (int i = 0; i <= 10; i++)
        {
            var name = $"Page{i}";
            var folder = Path.Combine(DeployRoot, $"website{siteNumber}", name);

            var slot = new PageSlot { Name = name, Index = i };

            if (Directory.Exists(folder))
            {
                var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                slot.FileCount = files.Length;
                slot.HasContent = files.Length > 0;
                slot.HasDefaultAspx = files.Any(f =>
                    Path.GetFileName(f).Equals("Default.aspx", StringComparison.OrdinalIgnoreCase));

                if (files.Length > 0)
                {
                    slot.LastModified = files
                        .Select(f => File.GetLastWriteTimeUtc(f))
                        .Max();
                }
            }

            pages.Add(slot);
        }

        return pages;
    }

    public List<FileNode> GetFileTree(int siteNumber, string pageName)
    {
        var folder = Path.Combine(DeployRoot, $"website{siteNumber}", pageName);
        if (!Directory.Exists(folder))
            return new List<FileNode>();

        return BuildTree(folder, folder);
    }

    public string? ReadFileContent(int siteNumber, string pageName, string relativePath)
    {
        var folder = Path.Combine(DeployRoot, $"website{siteNumber}", pageName);
        var fullPath = Path.GetFullPath(Path.Combine(folder, relativePath));

        // Safety: ensure path is within the expected folder
        if (!fullPath.StartsWith(Path.GetFullPath(folder), StringComparison.OrdinalIgnoreCase))
            return null;

        if (!File.Exists(fullPath))
            return null;

        return File.ReadAllText(fullPath);
    }

    private static List<FileNode> BuildTree(string currentDir, string rootDir)
    {
        var nodes = new List<FileNode>();

        foreach (var dir in Directory.GetDirectories(currentDir).OrderBy(d => d))
        {
            var info = new DirectoryInfo(dir);
            var node = new FileNode
            {
                Name = info.Name,
                RelativePath = Path.GetRelativePath(rootDir, dir).Replace('\\', '/'),
                IsDirectory = true,
                LastModified = info.LastWriteTimeUtc,
                Children = BuildTree(dir, rootDir)
            };
            nodes.Add(node);
        }

        foreach (var file in Directory.GetFiles(currentDir).OrderBy(f => f))
        {
            var info = new FileInfo(file);
            nodes.Add(new FileNode
            {
                Name = info.Name,
                RelativePath = Path.GetRelativePath(rootDir, file).Replace('\\', '/'),
                IsDirectory = false,
                Size = info.Length,
                LastModified = info.LastWriteTimeUtc
            });
        }

        return nodes;
    }

    public static string GetLanguageFromExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".aspx" => "aspnet",
            ".asax" => "aspnet",
            ".ascx" => "aspnet",
            ".master" => "aspnet",
            ".cs" => "csharp",
            ".css" => "css",
            ".js" => "javascript",
            ".json" => "json",
            ".html" or ".htm" => "html",
            ".xml" or ".config" or ".csproj" or ".sln" => "xml",
            ".sql" => "sql",
            ".md" => "markdown",
            _ => "plaintext"
        };
    }
}
