using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using WorkspaceService.Models;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record RemoveUserRequest(int UserId, int WorkspaceId);

public class RemoveUserValidator : AbstractValidator<RemoveUserRequest>
{
    public RemoveUserValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID cannot be empty.");
        
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID cannot be empty.");
    }
}

public class RemoveUserHandler
{
    private readonly WorkspaceManager _workspaceManager;
    
    public RemoveUserHandler(WorkspaceManager workspaceManager)
    {
        _workspaceManager = workspaceManager;
    }
    
    public async Task<ApiResult<bool>> Handle(HttpContext context, RemoveUserRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requesterId = Convert.ToInt32(context.User.FindFirst("id")?.Value);

        var workspace = await _workspaceManager.GetWorkspaceByIdIncludeUsersAsync(request.WorkspaceId);
        if (workspace == null)
        {
            return new ApiResult<bool>(false, false, "Workspace not found.");
        }

        var requester = workspace.Users.FirstOrDefault(x => x.UserId == requesterId);
        var target = workspace.Users.FirstOrDefault(x => x.UserId == request.UserId);

        if (requester == null)
            return new ApiResult<bool>(false, false, "You are not a member of this workspace.");

        if (target == null)
            return new ApiResult<bool>(false, false, "User not found in workspace.");

        // Self-removal allowed for managers and guests only
        if (requesterId == request.UserId)
        {
            if (requester.Role == Role.Owner)
            {
                if (workspace.Users.Count(u => u.Role == Role.Owner) == 1)
                    return new ApiResult<bool>(false, false, "You cannot remove yourself as the last owner. Promote someone else first.");
            }

            // Let managers/guests remove themselves
            var removed = await _workspaceManager.RemoveUserFromWorkspaceAsync(request.WorkspaceId, request.UserId);
            return new ApiResult<bool>(removed);
        }

        // Otherwise — permission checks for removing others
        if (requester.Role == Role.Guest)
            return new ApiResult<bool>(false, false, "You do not have permission to remove users.");

        if (requester.Role == Role.Manager && target.Role != Role.Guest)
            return new ApiResult<bool>(false, false, "Managers can only remove guests.");

        if (target.Role == Role.Owner && workspace.Users.Count(x => x.Role == Role.Owner) == 1)
            return new ApiResult<bool>(false, false, "You cannot remove the last owner of the workspace.");

        // All checks passed, remove the user
        var success = await _workspaceManager.RemoveUserFromWorkspaceAsync(request.WorkspaceId, request.UserId);
        return new ApiResult<bool>(success);
    }
}

public class RemoveUserEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workspaces/remove-user",
                async (HttpContext context, RemoveUserRequest request, RemoveUserHandler handler, RemoveUserValidator validator, CancellationToken cancellationToken) =>
                {
                    var validationResult = await validator.ValidateAsync(request, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                        return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages));
                    }

                    var response = await handler.Handle(context, request, cancellationToken);

                    return response.Success
                        ? Results.Ok(response)
                        : Results.NotFound(response);
                })
            .WithName("RemoveUser")
            .RequireAuthorization();
    }
}
