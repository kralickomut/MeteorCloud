namespace AuditService.Persistence.Entities;

public record AuditEvent
{
    public int Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;

    public int WorkspaceId { get; set; } = default;
    public int PerformedByUserId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; init; } = new();
}