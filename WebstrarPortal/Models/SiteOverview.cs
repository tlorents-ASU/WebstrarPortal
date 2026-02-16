namespace WebstrarPortal.Models;

public class SiteOverview
{
    public int SiteNumber { get; set; }
    public List<string> Asurites { get; set; } = new();
    public List<PageSlot> Pages { get; set; } = new();
    public int TotalFiles => Pages.Sum(p => p.FileCount);
    public DateTime? LastActivity => Pages
        .Where(p => p.LastModified.HasValue)
        .Select(p => p.LastModified!.Value)
        .DefaultIfEmpty()
        .Max();
}
