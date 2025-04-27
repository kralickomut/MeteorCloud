namespace MeteorCloud.Messaging.Events.File;

public class FolderDeletedEvent : BaseEvent
{
    public required string Path { get; init; }
}