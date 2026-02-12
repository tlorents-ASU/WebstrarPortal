namespace WebstrarPortal.Models;

public class PageSlot
{
    public string Name { get; set; } = "";
    public int Index { get; set; }
    public bool HasContent { get; set; }
    public int FileCount { get; set; }
    public DateTime? LastModified { get; set; }
}
