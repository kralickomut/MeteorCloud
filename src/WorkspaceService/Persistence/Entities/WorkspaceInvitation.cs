using WorkspaceService.Models;

namespace WorkspaceService.Persistence.Entities;

public class WorkspaceInvitation
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Email { get; set; } = default!;
    public int InvitedByUserId { get; set; }
    public Role Role { get; set; } 
    public Guid Token { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedOn { get; set; }
    public DateTime? AcceptedOn { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? AcceptedByUserId { get; set; }
}