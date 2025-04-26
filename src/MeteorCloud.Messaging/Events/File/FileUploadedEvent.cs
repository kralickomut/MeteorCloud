namespace MeteorCloud.Messaging.Events.File;

public class FileUploadedEvent : BaseEvent
{
    public required string FileId { get; init; }
    public required string FileName { get; init; }
    public required int WorkspaceId { get; init; }   
    public required string FolderPath { get; init; } 
    public required string ContentType { get; init; }
    
    public required long Size { get; init; }
    public required DateTime UploadedAt { get; init; }
    public required int UploadedBy { get; init; }
}