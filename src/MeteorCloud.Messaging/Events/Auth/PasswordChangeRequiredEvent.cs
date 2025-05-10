namespace MeteorCloud.Messaging.Events.Auth;

public class PasswordResetRequiredEvent : BaseEvent
{
    public string Email { get; set; } = string.Empty;
    public Guid Token { get; init; } = Guid.Empty;
}