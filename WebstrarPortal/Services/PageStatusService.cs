using WebstrarPortal.Models;

namespace WebstrarPortal.Services;

public class PageStatusService
{
    /// <summary>
    /// Returns the status of all 11 page slots (Page0–Page10) for a given site number.
    /// </summary>
    public List<PageSlot> GetPageStatuses(int siteNumber)
    {
        var pages = new List<PageSlot>();

        for (int i = 0; i <= 10; i++)
        {
            var name = $"Page{i}";
            var folder = Path.Combine(@"C:\WebstrarDeploy", $"website{siteNumber}", name);

            var slot = new PageSlot { Name = name, Index = i };

            if (Directory.Exists(folder))
            {
                var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                slot.FileCount = files.Length;
                slot.HasContent = files.Length > 0;

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
}
