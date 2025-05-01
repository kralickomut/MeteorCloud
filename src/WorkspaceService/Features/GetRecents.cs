using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using WorkspaceService.Persistence;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record GetRecentsRequest(int UserId);

public class GetRecentsValidator : AbstractValidator<GetRecentsRequest>
{
    public GetRecentsValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.")
            .GreaterThan(0)
            .WithMessage("UserId must be greater than 0.");
    }
}

public class GetRecentsHandler
{
    private readonly WorkspaceManager _workspaceManager;
    
    public GetRecentsHandler(WorkspaceManager workspaceManager)
    {
        _workspaceManager = workspaceManager;
    }
    
    public async Task<ApiResult<IEnumerable<Workspace>>> Handle(GetRecentsRequest request, CancellationToken cancellationToken)
    {
        var recents = await _workspaceManager.GetRecentsAsync(request.UserId, cancellationToken);
        return new ApiResult<IEnumerable<Workspace>>(recents);
    }
}

public class GetRecentsEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("api/workspaces/recent/{userId:int}", 
            async (int userId, GetRecentsValidator validator, GetRecentsHandler handler, CancellationToken cancellationToken) =>
            {
                var request = new GetRecentsRequest(userId);

                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(x => x.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errors));
                }

                var result = await handler.Handle(request, cancellationToken);
                return Results.Ok(result);
            });
    }
}