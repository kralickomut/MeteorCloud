using System.Data;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.Http.HttpResults;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record DeleteWorkspaceRequest(int Id);

public class DeleteWorkspaceRequestValidator : AbstractValidator<DeleteWorkspaceRequest>
{
    public DeleteWorkspaceRequestValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}


public class DeleteWorkspaceHandler
{
    private readonly WorkspaceManager _workspaceManager;

    public DeleteWorkspaceHandler(WorkspaceManager workspaceManager)
    {
        _workspaceManager = workspaceManager;
    }

    public async Task<ApiResult<object>> Handle(DeleteWorkspaceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var success = await _workspaceManager.DeleteWorkspaceAsync(request.Id, cancellationToken);

        return success
            ? new ApiResult<object>(null)
            : new ApiResult<object>(null, false, "Workspace not found");
    }
}

public static class DeleteWorkspaceEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/workspace/{id}",
            async (int id, DeleteWorkspaceHandler handler, DeleteWorkspaceRequestValidator validator, CancellationToken cancellationToken) =>
            {
                var request = new DeleteWorkspaceRequest(id);
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                
                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Results.BadRequest(new ApiResult<object>(null, false, string.Join(", ", errorMessages)));
                }
                
                var result = await handler.Handle(request, cancellationToken);
                
                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
                
            });
    }
}