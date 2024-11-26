using MassTransit;
using Messaging.Base.Events;

namespace FileService.Application.Consumers;

public class FileUploadedConsumer : IConsumer<FileUploadedEvent>
{
    public Task Consume(ConsumeContext<FileUploadedEvent> context)
    {
        var message = context.Message;
        Console.WriteLine($"File uploaded: {message.FileName}, Size: {message.FileSize}");
        return Task.CompletedTask;
    }
}