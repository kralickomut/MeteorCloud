namespace MeteorCloud.Messaging.Events.File;

public class FileDeletedEvent : BaseEvent
{
    public required string FileId { get; set; }
}