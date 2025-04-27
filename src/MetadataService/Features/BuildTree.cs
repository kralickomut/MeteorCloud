using FluentValidation;
using MetadataService.Models.Tree;
using MetadataService.Services;
using MeteorCloud.Communication;
using MeteorCloud.Shared.ApiResults;

namespace MetadataService.Features;

public record BuildTreeRequest(int WorkspaceId);

public class BuildTreeRequestValidator : AbstractValidator<BuildTreeRequest>
{
    public BuildTreeRequestValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID cannot be empty.")
            .GreaterThan(0)
            .WithMessage("Workspace ID must be greater than 0.");
    }
}

public class BuildTreeHandler
{
    private readonly IFileMetadataManager _fileMetadataManager;
    private readonly ILogger<BuildTreeHandler> _logger;
    private readonly MSHttpClient _httpClient;

    public BuildTreeHandler(IFileMetadataManager fileMetadataManager, ILogger<BuildTreeHandler> logger, MSHttpClient httpClient)
    {
        _fileMetadataManager = fileMetadataManager;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ApiResult<FolderNode>> Handle(BuildTreeRequest request, CancellationToken cancellationToken)
    {
        var url = MicroserviceEndpoints.WorkspaceService.GetWorkspaceById(request.WorkspaceId);
        var response = await _httpClient.GetAsync<object>(url);
        
        if (!response.Success || response.Data is null)
        {
            _logger.LogWarning("Workspace {WorkspaceId} not found", request.WorkspaceId);
            return new ApiResult<FolderNode>(null, false, "Workspace not found");
        }
        
        var tree = await _fileMetadataManager.BuildTreeAsync(request.WorkspaceId, cancellationToken);
        
        if (tree is null)
        {
            _logger.LogWarning("Tree users for workspace {WorkspaceId} not found", request.WorkspaceId);
            return new ApiResult<FolderNode>(null, false, "Users for tree not found.");
        }
        
        return new ApiResult<FolderNode>(tree);
    }
}


public class BuildTreeEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/metadata/tree/{workspaceId}", async (
            int workspaceId,
            BuildTreeRequestValidator validator,
            BuildTreeHandler handler,
            CancellationToken cancellationToken) =>
        {
            var request = new BuildTreeRequest(workspaceId);
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages));
            }

            var result = await handler.Handle(request, cancellationToken);
            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(result);
        }).RequireAuthorization();
    }
}