namespace MeteorCloud.API.DTOs.Workspace;

public class WorkspaceCreateRequest
{
    public int OwnerId { get; set; }
    
    public string Name { get; set; }
    
    public string Description { get; set; }
}