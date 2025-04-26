namespace MetadataService.Persistence.Entities;

public class FileMetadata
{
    public Guid Id { get; set; }                     // FileId = Blob name part
    public string FileName { get; set; } = default!;
    public string Path { get; set; } = default!;     // Folder path relative to root (e.g., "Assets/Icons")
    public int WorkspaceId { get; set; }
    public int UploadedBy { get; set; }
    
    public bool IsFolder { get; set; } = false;
    public long Size { get; set; }
    public string ContentType { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}