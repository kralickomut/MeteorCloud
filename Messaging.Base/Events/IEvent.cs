namespace Messaging.Base.Events;

public interface IEvent
{
    public DateTime Timestamp { get; }
}