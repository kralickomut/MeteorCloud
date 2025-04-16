namespace MeteorCloud.Messaging.Events.Workspace;

public class WorkspaceInvitationMatchOnRegisterEvent : BaseEvent
{
    public int UserId { get; set; }
    public string WorkspaceName { get; set; } 

    public WorkspaceInvitationMatchOnRegisterEvent(int userId, string workspaceName)
    {
        UserId = userId;
        WorkspaceName = workspaceName;
    }
}