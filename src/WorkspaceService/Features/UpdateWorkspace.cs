using FluentValidation;
using MeteorCloud.Communication;
using MeteorCloud.Shared.ApiResults;
using WorkspaceService.Persistence;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record UpdateWorkspaceRequest(int Id, string Name, string Description, int OwnerId);

public class UpdateWorkspaceRequestValidator : AbstractValidator<UpdateWorkspaceRequest>
{
    public UpdateWorkspaceRequestValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OwnerId).GreaterThan(0);
    }
}

public class UpdateWorkspaceHandler
{
    private readonly WorkspaceManager _workspaceManager;
    private readonly MSHttpClient _httpClient;
    
    public UpdateWorkspaceHandler(WorkspaceManager workspaceManager, MSHttpClient httpClient)
    {
        _workspaceManager = workspaceManager;
        _httpClient = httpClient;
    }
    
    public async Task<ApiResult<bool>> Handle(UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var userExists = await _httpClient.CheckUserExistsAsync(request.OwnerId, cancellationToken);
        
        if (userExists is false)
        {
            return new ApiResult<bool>(false, false, "User not found");
        }
        
        var workspace = new Workspace
        {
            Id = request.Id,
            Name = request.Name,
            Description = request.Description,
            OwnerId = request.OwnerId
        };
        
        var success = await _workspaceManager.UpdateWorkspaceAsync(workspace, cancellationToken);
        
        return success
            ? new ApiResult<bool>(true, true, "Workspace updated successfully")
            : new ApiResult<bool>(false, false, "Workspace update failed");
    }
}

public static class UpdateWorkspaceEndpoint 
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/workspace",
            async (UpdateWorkspaceRequest request, UpdateWorkspaceHandler handler, UpdateWorkspaceRequestValidator validator, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                
                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Results.BadRequest(new ApiResult<bool>(false, false, string.Join(", ", errorMessages)));
                }
                
                var result = await handler.Handle(request, cancellationToken);
                
                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
                
            });
    }

}