namespace MeteorCloud.Shared.ApiResults.SharedDto;

public class WorkspaceInvitationHistoryDto
{
    public string Email { get; set; }
    public string InvitedByName { get; set; }
    public string AcceptedByName { get; set; }
    public string Status { get; set; }
    public DateTime Date { get; set; }
    
    public DateTime? AcceptedOn { get; set; }
}