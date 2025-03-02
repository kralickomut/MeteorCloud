namespace MeteorCloud.Messaging.Events.Workspace;

public class WorkspaceDeletedEvent
{
    public int Id { get; }
    public IEnumerable<int> UserIds { get; }
    
    public WorkspaceDeletedEvent(int id, IEnumerable<int> userIds)
    {
        Id = id;
        UserIds = userIds;
    }
}