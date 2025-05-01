using MassTransit;
using MeteorCloud.Messaging.Events.File;
using WorkspaceService.Services;
using WorkspaceService.Utils;

namespace WorkspaceService.Consumers;

public class FileDeletedConsumer : IConsumer<FileDeletedEvent>
{
    private readonly ILogger<FileDeletedConsumer> _logger;
    private readonly WorkspaceManager _workspaceManager;
    
    public FileDeletedConsumer(ILogger<FileDeletedConsumer> logger, WorkspaceManager workspaceManager)
    {
        _logger = logger;
        _workspaceManager = workspaceManager;
    }
    
    public async Task Consume(ConsumeContext<FileDeletedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("FileDeletedEvent consumed: {MessageId}", message.Id);

        var workspace = await _workspaceManager.GetWorkspaceByIdAsync(message.WorkspaceId);

        if (workspace == null)
        {
            _logger.LogWarning("Workspace not found for ID: {WorkspaceId}", message.WorkspaceId);
            return;
        }

        workspace.SizeInGB = Math.Round(FileSizeUtils.SubtractSafe(workspace.SizeInGB, message.Size), 3);
        workspace.TotalFiles = Math.Max(0, workspace.TotalFiles - 1);

        await _workspaceManager.UpdateWorkspaceAsync(workspace);

        _logger.LogInformation("Workspace {WorkspaceId} updated: -1 file, -{SizeInGb:F3} GB", message.WorkspaceId, FileSizeUtils.BytesToGB(message.Size));
    }
}