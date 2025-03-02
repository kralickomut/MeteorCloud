namespace MeteorCloud.Messaging.Events.Workspace;

public class WorkspaceCreatedEvent : BaseEvent
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    
    public WorkspaceCreatedEvent(int id, int ownerId)
    {
        Id = id;
        OwnerId = ownerId;
    }
}