namespace Messaging.Base.Abstraction;

public interface IEventBus
{
    Task PublishAsync<T>(T @event) where T : class;
}