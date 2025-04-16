namespace MeteorCloud.Shared.ApiResults.SharedDto;

public class WorkspaceUserModel
{
    public int Id { get; set; }
    
    public int WorkspaceId { get; set; }
    
    public int UserId { get; set; }

    public int Role { get; set; } // Owner, Manager, Guest
}