namespace MeteorCloud.Messaging.Events;

public class UserNameChangedEvent : BaseEvent
{
    public int UserId { get; set; }
    public string NewName { get; set; } = string.Empty;
}