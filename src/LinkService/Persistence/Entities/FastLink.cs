namespace LinkService.Persistence.Entities;

public class FastLink
{
    public int Id { get; set; }
    public Guid Token { get; set; } 
    public Guid FileId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Name { get; set; } = null!;
    public string FileName { get; set; } = null!;
    
    public long FileSize { get; set; }
    public int AccessCount { get; set; }
}