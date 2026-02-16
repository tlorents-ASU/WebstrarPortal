namespace WebstrarPortal.Models;

public class CodeViewerViewModel
{
    public int SiteNumber { get; set; }
    public string PageName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Content { get; set; } = "";
    public string Language { get; set; } = "";
}
