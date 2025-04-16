namespace WorkspaceService.Persistence;

public class Workspace
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public required string Name { get; set; }
    
    public string OwnerName { get; set; }
    
    public string Status { get; set; }
    
    public double SizeInGB { get; set; }
    
    public int TotalFiles { get; set; }
    
    public string? Description { get; set; }
    
    public DateTime CreatedOn { get; set; }
    
    public DateTime? LastUploadOn { get; set; }
    
    
    // Navigation properties
    public List<WorkspaceUser>? Users { get; set; } = new();
}
