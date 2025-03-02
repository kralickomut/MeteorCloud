using System.ComponentModel.DataAnnotations;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using WorkspaceService.Persistence;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record CreateWorkspaceRequest(string Name, string Description, int OwnerId);

public record CreateWorkspaceResponse(int WorkspaceId);

public class CreateWorkspaceRequestValidator : AbstractValidator<CreateWorkspaceRequest>
{
    public CreateWorkspaceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OwnerId).GreaterThan(0);
    }
}

public class CreateWorkspaceHandler
{
    private readonly WorkspaceManager _workspaceManager;

    public CreateWorkspaceHandler(WorkspaceManager workspaceManager)
    {
        _workspaceManager = workspaceManager;
    }

    public async Task<ApiResult<CreateWorkspaceResponse>> Handle(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var workspace = new Workspace
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = request.OwnerId
        };
        
        var workspaceId = await _workspaceManager.CreateWorkspaceAsync(workspace, cancellationToken);
        
        return workspaceId.HasValue
            ? new ApiResult<CreateWorkspaceResponse>(new CreateWorkspaceResponse(workspaceId.Value), true, "Workspace created successfully")
            : new ApiResult<CreateWorkspaceResponse>(null, false, "Workspace creation failed");
    }
}


public static class CreateWorkspaceEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workspace", async (CreateWorkspaceRequest request, CreateWorkspaceRequestValidator validator, CancellationToken cancellationToken, CreateWorkspaceHandler handler) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, false));
            }
            
            var response = await handler.Handle(request, cancellationToken);
            
            return response.Success == true
                ? Results.Ok(response)
                : Results.BadRequest(response);
        });
    }
}









