using AuditService.Persistence;
using AuditService.Persistence.Entities;
using MassTransit;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.File;
using MeteorCloud.Shared.SharedDto.Users;

namespace AuditService.Consumers.File;

public class FileUploadedConsumer : IConsumer<FileUploadedEvent>
{
    
    private readonly ILogger<FileUploadedConsumer> _logger;
    private readonly AuditRepository _repository;
    private readonly MSHttpClient _httpClient;
    
    public FileUploadedConsumer(ILogger<FileUploadedConsumer> logger, AuditRepository repository, MSHttpClient httpClient)
    {
        _logger = logger;
        _repository = repository;
        _httpClient = httpClient;
    }
    
    public async Task Consume(ConsumeContext<FileUploadedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("Received FileUploadedEvent: {FileId} in workspace {WorkspaceId}", message.Id, message.WorkspaceId);
        
        var audit = new AuditEvent
        {
            EntityType = "File",
            EntityId = message.Id.ToString(),
            Action = "Uploaded",
            PerformedByUserId = message.UploadedBy,
            WorkspaceId = message.WorkspaceId,
            Timestamp = message.UploadedAt,
            Metadata = new Dictionary<string, string>
            {
                { "FileName", message.FileName }
            }
        };
        
        await _repository.InsertAsync(audit);
    }
}