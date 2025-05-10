namespace MeteorCloud.Messaging.Events.FastLink;

public class FastLinkExpireCleanupEvent : BaseEvent
{
    public List<ExpiredFastLinkDto> ExpiredLinks { get; set; }
}


public class ExpiredFastLinkDto
{
    public int UserId { get; set; }
    public Guid FileId { get; set; }
}