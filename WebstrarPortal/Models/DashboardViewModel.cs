namespace WebstrarPortal.Models;

public class DashboardViewModel
{
    public string Asurite { get; set; } = "";
    public int SiteNumber { get; set; }
    public List<PageSlot> Pages { get; set; } = new();
    public string ServiceBaseUrl { get; set; } = "";
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
