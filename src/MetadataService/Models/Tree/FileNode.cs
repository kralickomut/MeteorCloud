namespace MetadataService.Models.Tree;

public class FileNode
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
    public string UploadedByName { get; set; }
    public string ContentType { get; set; } = default!;
    
    public long Size { get; set; }
}