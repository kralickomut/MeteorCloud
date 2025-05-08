namespace MeteorCloud.Messaging.Events.FastLink;

public class FastLinkFileDeletedEvent : BaseEvent
{
    public Guid FileId { get; set; }
    public int UserId { get; set; }
}