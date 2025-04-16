namespace MeteorCloud.Messaging.Events.Workspace;

public class WorkspaceInvitationMatchOnRegisterEvent : BaseEvent
{
    public int UserId { get; set; }
    public string WorkspaceName { get; set; } 
    public int WorkspaceId { get; set; }
    public string Token { get; set; } 

    public WorkspaceInvitationMatchOnRegisterEvent(int userId, string workspaceName, string token, int workspaceId)
    {
        UserId = userId;
        WorkspaceName = workspaceName;
        Token = token;
        WorkspaceId = workspaceId;
    }
}