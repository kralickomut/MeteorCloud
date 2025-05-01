namespace MeteorCloud.Shared.SharedDto.Audit;

public class AuditEventModel
{
    public int AuditEventId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ActionByName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}