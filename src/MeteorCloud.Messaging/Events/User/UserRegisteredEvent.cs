namespace MeteorCloud.Messaging.Events;

public class UserRegisteredEvent : BaseEvent
{
    public int UserId { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public UserRegisteredEvent(int userId, string email, string firstName, string lastName)
    {
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
}