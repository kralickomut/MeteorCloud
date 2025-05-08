namespace MeteorCloud.Messaging.Events.File;

public class FileMovedEvent
{
    public string SourcePath { get; set; } = string.Empty;
    public string TargetFolder { get; set; } = string.Empty;
    public int WorkspaceId { get; set; }
    public int MovedBy { get; set; }
    
    public Guid FileId { get; set; }
}