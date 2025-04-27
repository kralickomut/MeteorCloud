namespace MeteorCloud.Messaging.Events.Workspace;

public class WorkspaceDeletedEvent : BaseEvent
{
    public int WorkspaceId { get; }
    public IEnumerable<int> UserIds { get; }
    
    public WorkspaceDeletedEvent(int workspaceId, IEnumerable<int> userIds)
    {
        WorkspaceId = workspaceId;
        UserIds = userIds;
    }
}