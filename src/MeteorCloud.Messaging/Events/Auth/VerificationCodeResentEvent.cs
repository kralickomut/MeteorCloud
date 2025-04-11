namespace MeteorCloud.Messaging.Events.Auth;

public class VerificationCodeResentEvent : BaseEvent
{
    public string Email { get; init; }
    public string VerificationCode { get; init; }
    
    public VerificationCodeResentEvent(string email, string verificationCode)
    {
        Email = email;
        VerificationCode = verificationCode;
    }
}