namespace MeteorCloud.Messaging.Events;

public class UserLoggedInEvent : BaseEvent
{
    public int UserId { get; init; }

    public UserLoggedInEvent(int userId)
    {
        UserId = userId;
    }
}