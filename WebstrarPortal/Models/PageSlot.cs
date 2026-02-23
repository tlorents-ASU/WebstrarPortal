namespace WebstrarPortal.Models;

public class PageSlot
{
    public string Name { get; set; } = "";
    public int Index { get; set; }
    public bool HasContent { get; set; }
    public int FileCount { get; set; }
    public DateTime? LastModified { get; set; }
    public bool HasDefaultAspx { get; set; }

    /// <summary>
    /// Best entry point file: Default.aspx if exists, else the single root .aspx, else null.
    /// </summary>
    public string? EntryFile { get; set; }

    /// <summary>
    /// All root-level .aspx filenames in this page folder.
    /// </summary>
    public List<string> AspxFiles { get; set; } = new();
}
