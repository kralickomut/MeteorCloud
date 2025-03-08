namespace MeteorCloud.API.DTOs.Workspace;

public class WorkspaceUpdateRequest
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}