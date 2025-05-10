using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using WorkspaceService.Persistence;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record GetWorkspaceByIdRequest(int Id);

public class GetWorkspaceByIdValidator : AbstractValidator<GetWorkspaceByIdRequest>
{
    public GetWorkspaceByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Workspace ID is required.");
    }
}

public class GetWorkspaceByIdHandler
{
    private readonly WorkspaceManager _workspaceManager;

    public GetWorkspaceByIdHandler(WorkspaceManager workspaceManager)
    {
        _workspaceManager = workspaceManager;
    }
    
    public async Task<ApiResult<Workspace>> Handle(GetWorkspaceByIdRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var workspace = await _workspaceManager.GetWorkspaceByIdIncludeUsersAsync(request.Id);
        
        if (workspace == null)
        {
            return new ApiResult<Workspace>( null, false, "Workspace not found.");
        }

        return new ApiResult<Workspace>(workspace, true, "Workspace retrieved successfully.");
    }
}

public class GetWorkspaceByIdEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/workspaces/{id}", 
            async (
                int id,
                GetWorkspaceByIdHandler handler,
                GetWorkspaceByIdValidator validator,
                CancellationToken cancellationToken) =>
            {
                var request = new GetWorkspaceByIdRequest(id);
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