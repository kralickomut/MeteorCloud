using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using WorkspaceService.Models;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record ChangeUserRoleRequest(
    int WorkspaceId,
    int UserId,
    int Role);
    
    
public class ChangeUserRoleValidator : AbstractValidator<ChangeUserRoleRequest>
{
    public ChangeUserRoleValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID cannot be empty.");
        
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID cannot be empty.");
        
        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Role cannot be empty.")
            .Must(role => role == 1 || role == 2 || role == 3)
            .WithMessage("Invalid role. Valid roles are: 1 (Owner), 2 (Manager), 3 (Guest).");
    }
}


public class ChangeUserRoleHandler
{
    private readonly WorkspaceManager _workspaceManager;
    
    public ChangeUserRoleHandler(WorkspaceManager workspaceManager)
    {
        _workspaceManager = workspaceManager;
    }
    
    public async Task<ApiResult<bool>> Handle(HttpContext context, ChangeUserRoleRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var userId = Convert.ToInt32(context.User.FindFirst("id")?.Value);

        var workspace = await _workspaceManager.GetWorkspaceByIdIncludeUsersAsync(request.WorkspaceId);

        if (workspace is null)
        {
            return new ApiResult<bool>(false, false, "Workspace not found.");
        }

        var workspaceUser = workspace.Users!.First(x => x.UserId == userId);
        
        if (workspaceUser.Role != Role.Owner && workspaceUser.Role != Role.Manager)
        {
            return new ApiResult<bool>(false, false, "You are not authorized to change user roles.");
        }
        
        var result = await _workspaceManager.ChangeUserRoleAsync(request.WorkspaceId, request.UserId, request.Role, userId, cancellationToken);

        return new ApiResult<bool>(result);
    }
}


public class ChangeUserRoleEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workspaces/change-user-role",
            async (HttpContext context, ChangeUserRoleRequest request, ChangeUserRoleHandler handler,
                ChangeUserRoleValidator validator, CancellationToken cancellationToken) =>
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
            });
    }
}