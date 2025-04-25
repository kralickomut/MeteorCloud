namespace MeteorCloud.Messaging.Events.Workspace;

public class WorkspaceUserRemovedEvent : BaseEvent
{
    public int WorkspaceId { get; set; }
    public int UserId { get; set; }
    
    
    public WorkspaceUserRemovedEvent(int workspaceId, int userId)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
    }
}