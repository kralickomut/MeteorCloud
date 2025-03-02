namespace WorkspaceService.Persistence;

public class Workspace
{
    public int Id { get; set; }
    
    public int OwnerId { get; set; }
    
    public required string Name { get; set; }
    
    public string? Description { get; set; }
    
    public DateTime CreatedOn { get; set; }
    
    public DateTime LastUploadOn { get; set; }

}