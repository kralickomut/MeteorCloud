using System.Text.Json;
using FluentValidation;
using MassTransit;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.Workspace;
using MeteorCloud.Shared.ApiResults;
using Npgsql;
using WorkspaceService.Persistence.Entities;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record InviteToWorkspaceRequest(string Email, int WorkspaceId, int invitedByUserId);

public class InviteToWorkspaceValidator : AbstractValidator<InviteToWorkspaceRequest>
{
    public InviteToWorkspaceValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Invalid email format.");
        
        RuleFor(x => x.WorkspaceId)
            .GreaterThan(0)
            .WithMessage("Workspace ID must be greater than 0.");
        
        RuleFor(x => x.invitedByUserId)
            .GreaterThan(0)
            .WithMessage("Invited By User ID must be greater than 0.");
    }
}

public class InviteToWorkspaceHandler
{
    private readonly WorkspaceManager _workspaceManager;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<InviteToWorkspaceHandler> _logger;
    private readonly MSHttpClient _httpClient;

    public InviteToWorkspaceHandler(WorkspaceManager workspaceManager, IPublishEndpoint publishEndpoint, ILogger<InviteToWorkspaceHandler> logger, MSHttpClient httpClient)
    {
        _workspaceManager = workspaceManager;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _httpClient = httpClient;
    }


    public async Task<ApiResult<bool>> Handle(InviteToWorkspaceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        
        // Check if workspace exists
        var workspace = await _workspaceManager.GetWorkspaceByIdAsync(request.WorkspaceId);

        if (workspace is null)
        {
            return new ApiResult<bool>(false, false, "Workspace not found.");
        }
        
        // Check if user exists
        var url = MicroserviceEndpoints.UserService.GetUserByEmail(request.Email);
        var response = await _httpClient.GetAsync<object>(url);
        
        bool userExists = response.Success;
        Console.WriteLine("RESPONSE DATA: " + response.Data);

        // If user exists, get user ID and check if they are already a member
        if (userExists)
        {
            int userId = ((JsonElement)response.Data!).GetInt32();
            
            var isAlreadyMember = await _workspaceManager.IsUserInWorkspaceAsync(userId, workspace.Id, cancellationToken);
            
            if (isAlreadyMember)
            {
                return new ApiResult<bool>(false, false, "User is already a member.");
            }
        }
        
        // Now invite the user to the workspace by email
        WorkspaceInvitation? invitation = null;

        try
        {
            invitation = await _workspaceManager.InviteToWorkspaceAsync(
                request.WorkspaceId,
                request.Email, 
                request.invitedByUserId,
                cancellationToken);
            
            if (invitation is null)
            {
                return new ApiResult<bool>(false, false, "User is already a member.");
            }
            
        }
        catch (PostgresException ex)
        {
            return new ApiResult<bool>(false, false, "This user has already been invited.");
        }
        catch (Exception ex)
        {
            return new ApiResult<bool>(false, false, "An unexpected error occurred.");
        }
        
        
        await _publishEndpoint.Publish(new WorkspaceInviteEvent(invitation.WorkspaceId, invitation.Email, invitation.InvitedByUserId, invitation.Token));
        _logger.LogInformation("📩 Invitation sent to {Email} for workspace {WorkspaceId}", invitation.Email, invitation.WorkspaceId);

        return new ApiResult<bool>(true, true, "Invitation sent successfully.");
    }
}


public class InviteToWorkspaceEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workspaces/invite", 
            async (InviteToWorkspaceRequest request, InviteToWorkspaceHandler handler, InviteToWorkspaceValidator validator, CancellationToken cancellationToken) =>
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
            });
        
    }
}