using FluentValidation;
using MetadataService.Persistence.Entities;
using MetadataService.Services;
using MeteorCloud.Communication;
using MeteorCloud.Shared.ApiResults;

namespace MetadataService.Features;

public record CreateFolderRequest(int WorkspaceId, string Name, string Path, int UploadedBy);

public class CreateFolderRequestValidator : AbstractValidator<CreateFolderRequest>
{
    public CreateFolderRequestValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Path).NotEmpty().MaximumLength(255);
    }
}

public class CreateFolderHandler
{
    private readonly IFileMetadataManager _fileMetadataManager;
    private readonly ILogger<CreateFolderHandler> _logger;
    private readonly MSHttpClient _httpClient;

    public CreateFolderHandler(IFileMetadataManager fileMetadataManager, ILogger<CreateFolderHandler> logger, MSHttpClient httpClient)
    {
        _fileMetadataManager = fileMetadataManager;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task Handle(CreateFolderRequest request, CancellationToken cancellationToken)
    {
        var url = MicroserviceEndpoints.WorkspaceService.IsUserInWorkspace(request.UploadedBy, request.WorkspaceId);
        var response = await _httpClient.GetAsync<bool>(url);
        
        if (!response.Success)
        {
            _logger.LogWarning("User {UserId} is not in workspace {WorkspaceId}", request.UploadedBy, request.WorkspaceId);
            throw new Exception("User is not authorized to create a folder in this workspace.");
        }
        
        
        var folderMetadata = new FileMetadata
        {
            Id = Guid.NewGuid(),
            WorkspaceId = request.WorkspaceId,
            Path = request.Path,
            UploadedBy = request.UploadedBy,
            ContentType = "folder",
            UploadedAt = DateTime.UtcNow,
            FileName = request.Name,
            IsFolder = false
        };

        await _fileMetadataManager.CreateAsync(folderMetadata, cancellationToken);
        _logger.LogInformation("Created folder with ID {Id} in workspace {WorkspaceId}", folderMetadata.Id, request.WorkspaceId);
    }
}


public class CreateFolderEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/folder", async 
                (
                    CreateFolderRequest request, 
                    IValidator<CreateFolderRequest> validator, 
                    CreateFolderHandler handler) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages));
            }

            await handler.Handle(request, CancellationToken.None);
            return Results.Created($"/api/folders/{request.Name}", request);
        })
        .WithName("CreateFolder")
        .Produces(201)
        .ProducesValidationProblem()
        .RequireAuthorization();
    }
}