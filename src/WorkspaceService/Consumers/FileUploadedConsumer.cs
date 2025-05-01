using MassTransit;
using MeteorCloud.Messaging.Events.File;
using WorkspaceService.Services;
using WorkspaceService.Utils;

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

        _logger.LogInformation("FileUploadedEvent consumed: {MessageId}", message.Id);

        var workspace = await _workspaceManager.GetWorkspaceByIdAsync(message.WorkspaceId);

        if (workspace == null)
        {
            _logger.LogWarning("Workspace not found for ID: {WorkspaceId}", message.WorkspaceId);
            return;
        }

        var newSize = FileSizeUtils.AddSafe(workspace.SizeInGB, message.Size);
        workspace.SizeInGB = Math.Round(newSize, 3);
        workspace.TotalFiles += 1;
        workspace.LastUploadOn = DateTime.UtcNow;

        var result = await _workspaceManager.UpdateWorkspaceAsync(workspace);
        
        if (!result)
        {
            _logger.LogWarning("Failed to update workspace {WorkspaceId}", message.WorkspaceId);
            return;
        }
        
        _logger.LogInformation("Workspace {WorkspaceId} updated: +1 file, +{SizeInGb:F3} GB", message.WorkspaceId, FileSizeUtils.BytesToGB(message.Size));
    }
}