using FluentValidation;
using MassTransit;
using MeteorCloud.Messaging.Events.Workspace;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.SignalR;
using WorkspaceService.Hubs;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record RespondToInviteRequest(Guid Token, bool Accept);

public class RespondToInviteValidator : AbstractValidator<RespondToInviteRequest>
{
    public RespondToInviteValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token cannot be empty.");

        RuleFor(x => x.Accept)
            .NotNull()
            .WithMessage("Accept cannot be empty.");
    }
    
}

public class RespondToInviteHandler
{
    private readonly WorkspaceManager _workspaceManager;
    private readonly ILogger<RespondToInviteHandler> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IHubContext<WorkspaceHub> _hubContext;

    public RespondToInviteHandler(
        WorkspaceManager workspaceManager,
        ILogger<RespondToInviteHandler> logger,
        IPublishEndpoint publishEndpoint,
        IHubContext<WorkspaceHub> hubContext)
    {
        _workspaceManager = workspaceManager;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _hubContext = hubContext;
    }
    
    public async Task<ApiResult<bool>> Handle(RespondToInviteRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _workspaceManager.RespondToInvitationAsync(request.Token, request.Accept, cancellationToken);

        if (!result.success)
        {
            return new ApiResult<bool>(false, false, "Failed to respond to invite.");
        }

        if (request.Accept)
        {
            // signalR
            var workspace = await _workspaceManager.GetWorkspaceByIdAsync(result.workspaceId!.Value);
            if (workspace == null)
            {
                throw new Exception("Workspace not found.");
            }
        
            // send workspace via signalR
            await _hubContext.Clients.User(result.userId.ToString()).SendAsync("WorkspaceJoined", workspace);
        }
        
        return new ApiResult<bool>(true, true, "Successfully responded to invite.");
    }
}


public static class RespondToInviteEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workspace/invite/respond",
            async (RespondToInviteRequest request, 
                RespondToInviteValidator validator, 
                RespondToInviteHandler handler, 
                CancellationToken cancellationToken) =>
            {
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
            }).WithName("RespondToInvite");
    }
}