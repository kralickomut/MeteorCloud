namespace MeteorCloud.Messaging.Events;

public class BaseEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public DateTime CreatedDateTime => DateTime.Now;
}