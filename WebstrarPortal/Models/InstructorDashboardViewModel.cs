namespace WebstrarPortal.Models;

public class InstructorDashboardViewModel
{
    public string Asurite { get; set; } = "";
    public int SiteNumber { get; set; }
    public List<SiteOverview> Sites { get; set; } = new();
    public string ServiceBaseUrl { get; set; } = "";
}
