namespace MeteorCloud.Messaging.Events.Workspace;

public class WorkspaceInvitationResponseEvent : BaseEvent
{
    public int UserId { get; set; }
    public int WorkspaceId { get; set; }
    
    public bool IsAccepted { get; set; }

    public WorkspaceInvitationResponseEvent(int userId, int workspaceId, bool isAccepted)
    {
        UserId = userId;
        WorkspaceId = workspaceId;
        IsAccepted = isAccepted;
    }
}