namespace MeteorCloud.Messaging.Events;

public class UserUpdatedEvent : BaseEvent
{
    public int UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }

    
    public UserUpdatedEvent(int userId, string firstName, string lastName, string email)
    {
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }
}