namespace MeteorCloud.Messaging.Events;

public class UserDeletedEvent : BaseEvent
{
    public int UserId { get; set; }

    public UserDeletedEvent(int userId)
    {
        UserId = userId;
    }
    
}