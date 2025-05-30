namespace MeteorCloud.Messaging.Events;

public class FailedEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public string? ExceptionMessage { get; set; }
    public string? StackTrace { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Processed { get; set; }
}