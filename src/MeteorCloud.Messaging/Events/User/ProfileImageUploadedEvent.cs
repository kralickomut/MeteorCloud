namespace MeteorCloud.Messaging.Events;

public class ProfileImageUploadedEvent : BaseEvent
{
    public int UserId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}