namespace MeteorCloud.Messaging.Events.Workspace;

public class WorkspaceInviteEvent : BaseEvent
{
    public int WorkspaceId { get; set; }
    public string Email { get; set; }
    public int InvitedByUserId { get; set; }
    public Guid Token { get; set; }

    public WorkspaceInviteEvent(int workspaceId, string email, int invitedByUserId, Guid token)
    {
        WorkspaceId = workspaceId;
        Email = email;
        InvitedByUserId = invitedByUserId;
        Token = token;
    }
}