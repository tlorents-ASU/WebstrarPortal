namespace WebstrarPortal.Models;

public class FileBrowserViewModel
{
    public int SiteNumber { get; set; }
    public string PageName { get; set; } = "";
    public List<string> Asurites { get; set; } = new();
    public List<FileNode> Files { get; set; } = new();
    public string SiteBaseUrl { get; set; } = "";
    public List<string> AspxFiles { get; set; } = new();
    public string? EntryFile { get; set; }
}
