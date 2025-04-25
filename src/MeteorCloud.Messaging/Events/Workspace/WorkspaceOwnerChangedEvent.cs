namespace MeteorCloud.Messaging.Events.Workspace;

public class WorkspaceOwnerChangedEvent : BaseEvent
{
    public int WorkspaceId { get; set; }
    public int OldOwnerId { get; set; }
    public int NewOwnerId { get; set; }
    public WorkspaceOwnerChangedEvent(int workspaceId, int oldOwnerId, int newOwnerId)
    {
        WorkspaceId = workspaceId;
        OldOwnerId = oldOwnerId;
        NewOwnerId = newOwnerId;
    }
}