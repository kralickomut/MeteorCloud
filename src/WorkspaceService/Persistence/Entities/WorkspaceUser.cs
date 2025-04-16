using WorkspaceService.Models;

namespace WorkspaceService.Persistence;

public class WorkspaceUser
{
    public int Id { get; set; }
    
    public int WorkspaceId { get; set; }
    
    public int UserId { get; set; }

    public Role Role { get; set; } = Role.Guest; // Owner, Manager, Guest

}