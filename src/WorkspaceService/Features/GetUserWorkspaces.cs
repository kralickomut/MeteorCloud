using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using WorkspaceService.Persistence;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record GetUserWorkspacesRequest(int UserId, int Page = 1, int PageSize = 10);

public class GetUserWorkspacesValidator : AbstractValidator<GetUserWorkspacesRequest>
{
    public GetUserWorkspacesValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("User ID must be greater than 0.");
        
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0.");
    }
}

public class GetUserWorkspacesHandler
{
    private readonly WorkspaceManager _workspaceManager;

    public GetUserWorkspacesHandler(WorkspaceManager workspaceManager)
    {
        _workspaceManager = workspaceManager;
    }
    
    public async Task<ApiResult<List<Workspace>>> Handle(GetUserWorkspacesRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var workspaces = await _workspaceManager.GetUserWorkspacesAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
        
        return new ApiResult<List<Workspace>>(workspaces);
    }
}

public class GetUserWorkspacesEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/workspaces/user/{userId}", 
            async (
                int userId,
                int page,
                int pageSize,
                GetUserWorkspacesHandler handler,
                GetUserWorkspacesValidator validator,
                CancellationToken cancellationToken) =>
            {
                var request = new GetUserWorkspacesRequest(userId, page, pageSize);
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