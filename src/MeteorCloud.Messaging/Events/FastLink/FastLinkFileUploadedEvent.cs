namespace MeteorCloud.Messaging.Events.FastLink;

public class FastLinkFileUploadedEvent : BaseEvent
{
    public Guid FileId { get; set; }
    public int UserId { get; set; }
    public string FileName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public long FileSize { get; set; }
}