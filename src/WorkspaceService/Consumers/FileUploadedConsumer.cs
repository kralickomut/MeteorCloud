using MassTransit;
using MeteorCloud.Messaging.Events.File;
using WorkspaceService.Services;

namespace WorkspaceService.Consumers;

public class FileUploadedConsumer : IConsumer<FileUploadedEvent>
{
    
    private readonly ILogger<FileUploadedConsumer> _logger;
    private readonly WorkspaceManager _workspaceManager;
    
    public FileUploadedConsumer(ILogger<FileUploadedConsumer> logger, WorkspaceManager workspaceManager)
    {
        _logger = logger;
        _workspaceManager = workspaceManager;
    }
    
    public async Task Consume(ConsumeContext<FileUploadedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("FileUploadedEvent consumed: {Message}", message.Id);

        var workspace = await _workspaceManager.GetWorkspaceByIdAsync(message.WorkspaceId);
        
        if (workspace == null)
        {
            _logger.LogWarning("Workspace not found for ID: {WorkspaceId}", message.WorkspaceId);
            return;
        }
        
        _logger.LogInformation("Updating workspace size that came: {WorkspaceId}", message.Size);
        workspace.SizeInGB += (double)message.Size / 1_000_000_000;
        
        await _workspaceManager.UpdateWorkspaceAsync(workspace);
    }
}