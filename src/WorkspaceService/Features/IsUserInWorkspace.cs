using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record IsUserInWorkspaceRequest(int UserId, int WorkspaceId);

public class IsUserInWorkspaceRequestValidator : AbstractValidator<IsUserInWorkspaceRequest>
{
    public IsUserInWorkspaceRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be valid.");

        RuleFor(x => x.WorkspaceId)
            .GreaterThan(0).WithMessage("Workspace ID must be valid.");
    }
}

public class IsUserInWorkspaceHandler
{
    private readonly WorkspaceManager _workspaceService;

    public IsUserInWorkspaceHandler(WorkspaceManager workspaceService)
    {
        _workspaceService = workspaceService;
    }

    public async Task<ApiResult<bool>> Handle(IsUserInWorkspaceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var result = await _workspaceService.IsUserInWorkspaceAsync(request.UserId, request.WorkspaceId);
        
        return result
            ? new ApiResult<bool>(true, true, "User is in workspace.")
            : new ApiResult<bool>(false, false, "User is not in workspace.");
    }
}


public class IsUserInWorkspaceEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/workspaces/is-user-in-workspace/{userId}/{workspaceId}", async (
            int userId,
            int workspaceId,
            IsUserInWorkspaceHandler handler,
            IsUserInWorkspaceRequestValidator validator,
            CancellationToken cancellationToken) =>
        {
            var request = new IsUserInWorkspaceRequest(userId, workspaceId);
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
