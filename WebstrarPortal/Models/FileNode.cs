namespace WebstrarPortal.Models;

public class FileNode
{
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public List<FileNode> Children { get; set; } = new();
}
