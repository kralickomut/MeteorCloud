namespace EmailService.Persistence;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    public int? WorkspaceId { get; set; } // In case its workspace invitation
    
    public bool? IsAccepted { get; set; } // In case its workspace invitation
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}