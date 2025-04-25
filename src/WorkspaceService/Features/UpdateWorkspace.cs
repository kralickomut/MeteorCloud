using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record UpdateWorkspaceRequest(
    int WorkspaceId,
    string Name,
    string Description);


public class UpdateWorkspaceValidator : AbstractValidator<UpdateWorkspaceRequest>
{
    public UpdateWorkspaceValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID cannot be empty.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name cannot be empty.")
            .MaximumLength(30)
            .WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.");
    }
}    

public class UpdateWorkspaceHandler
{
    private readonly WorkspaceManager _workspaceManager;

    public UpdateWorkspaceHandler(WorkspaceManager workspaceManager)
    {
        _workspaceManager = workspaceManager;
    }
    
    public async Task<ApiResult<bool>> Handle(UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var workspace = await _workspaceManager.GetWorkspaceByIdAsync(request.WorkspaceId);

        if (workspace is null)
        {
            return new ApiResult<bool>(false, false, "Workspace not found.");
        }

        workspace.UpdatedOn = DateTime.UtcNow;
        workspace.Description = request.Description;
        workspace.Name = request.Name;

        var result = await _workspaceManager.UpdateWorkspaceAsync(workspace, cancellationToken);

        return new ApiResult<bool>(result);
    }
}


public class UpdateWorkspaceEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        
        app.MapPut("/api/workspaces/update", async 
            (UpdateWorkspaceRequest request, UpdateWorkspaceHandler handler, UpdateWorkspaceValidator validator, CancellationToken cancellationToken) =>
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