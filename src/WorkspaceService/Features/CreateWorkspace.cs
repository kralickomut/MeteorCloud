using System.Runtime.InteropServices.JavaScript;
using FluentValidation;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.Workspace;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.SignalR;
using WorkspaceService.Hubs;
using WorkspaceService.Persistence;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record CreateWorkspaceRequest(string Name, string Description, int OwnerId, string ownerName);

public record CreateWorkspaceResponse(int WorkspaceId);

public class CreateWorkspaceValidator : AbstractValidator<CreateWorkspaceRequest>
{
    public CreateWorkspaceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Workspace name is required.")
            .MaximumLength(30)
            .WithMessage("Workspace name must be less than 20 characters.");
        
        RuleFor(x => x.Description)
            .MaximumLength(100)
            .WithMessage("Workspace description must be less than 100 characters.");
        
        RuleFor(x => x.OwnerId)
            .NotEmpty()
            .GreaterThan(0)
            .WithMessage("Owner ID must be greater than 0.");

        RuleFor(x => x.ownerName)
            .NotEmpty()
            .WithMessage("Owner name is required.");
    }
}

public class CreateWorkspaceHandler
{
    private readonly WorkspaceManager _workspaceManager;

    public CreateWorkspaceHandler(WorkspaceManager workspaceManager)
    {
        _workspaceManager = workspaceManager;
    }
    
    public async Task<ApiResult<Workspace>> Handle(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var workspace = await _workspaceManager.CreateWorkspaceAsync(new Workspace
        {
            Name = request.Name,
            Description = request.Description,
            OwnerName = request.ownerName,
            OwnerId = request.OwnerId,
            Status = "Active",
            SizeInGB = 0,
            TotalFiles = 0,
            CreatedOn = DateTime.UtcNow,
            LastUploadOn = null
        }, cancellationToken);

        if (workspace is null)
        {
            return new ApiResult<Workspace>(null, false, "Failed to create workspace.");
        }
        
        return new ApiResult<Workspace>(workspace, true, "Workspace created successfully.");
    }
}

public class CreateWorkspaceEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workspaces", 
            async (HttpContext context, CreateWorkspaceRequest request, CreateWorkspaceHandler handler, CreateWorkspaceValidator validator, CancellationToken cancellationToken) =>
        {
            
            var userId = context.User.FindFirst("id")?.Value;
            
            if (!int.TryParse(userId, out var ownerId))
            {
                return Results.Unauthorized();
            }
            
            if (request.OwnerId != ownerId)
            {
                return Results.Unauthorized();
            }
            
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
        }).RequireAuthorization();
    }
}