using FluentValidation;
using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.SignalR;
using WorkspaceService.Hubs;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record DeleteWorkspaceRequest(int Id);

public class DeleteWorkspaceValidator : AbstractValidator<DeleteWorkspaceRequest>
{
    public DeleteWorkspaceValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Workspace ID is required.")
            .GreaterThan(0)
            .WithMessage("Workspace ID must be greater than 0.");
        
    }
}


public class DeleteWorkspaceHandler
{
    private readonly WorkspaceManager _workspaceManager;
    private readonly IHubContext<WorkspaceHub> _hubContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteWorkspaceHandler> _logger;

    public DeleteWorkspaceHandler(
        WorkspaceManager workspaceManager, 
        IHubContext<WorkspaceHub> hubContext, 
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteWorkspaceHandler> logger)
    {
        _workspaceManager = workspaceManager;
        _hubContext = hubContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public async Task<ApiResult<bool>> Handle(DeleteWorkspaceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var result = await _workspaceManager.DeleteWorkspaceAsync(request.Id, cancellationToken);

        if (result is not null)
        {
            var workspaceId = result.Value.workspaceId;
            var userIds = result.Value.userIds;
            
            await _hubContext.Clients.Users(userIds.Select(id => id.ToString()))
                .SendAsync("WorkspaceDeleted", new
                {
                    WorkspaceId = workspaceId
                });
            
            
            await _publishEndpoint.Publish(new WorkspaceDeletedEvent(workspaceId, userIds));
            _logger.LogInformation("Workspace with id: {WorkspaceId} DELETED", workspaceId);
        }
        
        return result is not null
            ? new ApiResult<bool>(true, true, "Workspace deleted successfully.")
            : new ApiResult<bool>(false, false, "Failed to delete workspace.");
    }
}


public static class DeleteWorkspaceEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/workspaces/{id:int}", 
            async (int id, HttpContext context, DeleteWorkspaceHandler handler, DeleteWorkspaceValidator validator, CancellationToken cancellationToken) =>
        {
            // For future
            var userId = context.User.FindFirst("UserId")?.Value;
            
            var request = new DeleteWorkspaceRequest(id);
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages));
            }

            var response = await handler.Handle(request, cancellationToken);

            return response.Success
                ? Results.Ok(response)
                : Results.NotFound(response);
        }).RequireAuthorization();
    }
}