namespace MeteorCloud.Messaging.Events.Workspace;

public class WorkspaceInvitationAcceptedEvent : BaseEvent
{
    public int UserId { get; set; }
    public int WorkspaceId { get; set; }

    public WorkspaceInvitationAcceptedEvent(int userId, int workspaceId)
    {
        UserId = userId;
        WorkspaceId = workspaceId;
    }
}