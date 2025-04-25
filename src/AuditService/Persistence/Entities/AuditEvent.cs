namespace AuditService.Persistence.Entities;

public record AuditEvent
{
    public string EntityType { get; init; } // e.g. "Workspace", "File", "User"
    public string EntityId { get; init; }
    public string Action { get; init; }     // e.g. "Created", "Deleted", "Updated", "Invited"
    public string PerformedByUserId { get; init; }
    public string? PerformedByUserName { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, string>? Metadata { get; init; } // Optional: extra info
}