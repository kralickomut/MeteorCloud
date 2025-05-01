namespace MeteorCloud.Messaging.Events.File;

public class FileDeletedEvent : BaseEvent
{
    public required string FileId { get; set; }
    
    public required int WorkspaceId { get; set; }
    public required long Size { get; set; }
    
    public required string FileName { get; set; }
    public required int DeletedBy { get; set; }
}