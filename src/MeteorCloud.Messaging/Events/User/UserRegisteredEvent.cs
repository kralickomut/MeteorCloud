namespace MeteorCloud.Messaging.Events;

public class UserRegisteredEvent : BaseEvent
{
    public int UserId { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public string VerificationCode { get; init; }

    public UserRegisteredEvent(int userId, string email, string name, string verificationCode)
    {
        UserId = userId;
        Email = email;
        Name = name;
        VerificationCode = verificationCode;
    }
}