using FileService.Services;
using FluentValidation;
using MassTransit;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.File;
using MeteorCloud.Shared.ApiResults;

namespace FileService.Features;

public record MoveFileRequest(string SourcePath, string TargetFolder, int WorkspaceId, int RequestedBy);

public class MoveFileRequestValidator : AbstractValidator<MoveFileRequest>
{
    public MoveFileRequestValidator()
    {
        RuleFor(x => x.SourcePath).NotEmpty().WithMessage("SourcePath is required.");
        RuleFor(x => x.TargetFolder).NotNull().WithMessage("TargetFolder is required."); // allow "" for root
        RuleFor(x => x.WorkspaceId).GreaterThan(0).WithMessage("WorkspaceId must be valid.");
        RuleFor(x => x.RequestedBy).GreaterThan(0).WithMessage("RequestedBy must be valid.");
    }
}


public class MoveFileHandler
{
    private readonly BlobStorageService _blobStorageService;
    private readonly MSHttpClient _httpClient;
    private readonly IPublishEndpoint _publishEndpoint;

    public MoveFileHandler(BlobStorageService blobStorageService, MSHttpClient httpClient, IPublishEndpoint publishEndpoint)
    {
        _blobStorageService = blobStorageService;
        _httpClient = httpClient;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ApiResult<bool>> Handle(MoveFileRequest request, CancellationToken cancellationToken)
    {
        // Check workspace membership
        var url = MicroserviceEndpoints.WorkspaceService.IsUserInWorkspace(request.RequestedBy, request.WorkspaceId);
        var check = await _httpClient.GetAsync<bool>(url, cancellationToken);
        if (!check.Success || !check.Data)
        {
            return new ApiResult<bool>(false, false, "User is not a member of the workspace.");
        }
        
        if (!Guid.TryParse(request.SourcePath.Split("/").Last(), out var fileId))
        {
            return new ApiResult<bool>(false, false, "Invalid file ID.");
        }

        var newPath = await _blobStorageService.MoveFileAsync(
            request.SourcePath,
            request.TargetFolder,
            cancellationToken);
        
        if (string.IsNullOrEmpty(newPath))
        {
            return new ApiResult<bool>(false, false, "File move failed.");
        }
        
        // Publish file moved event
        var fileMovedEvent = new FileMovedEvent
        {
            SourcePath = request.SourcePath,
            TargetFolder = request.TargetFolder,
            WorkspaceId = request.WorkspaceId,
            MovedBy = request.RequestedBy,
            FileId = fileId
        };
        
        await _publishEndpoint.Publish(fileMovedEvent, cancellationToken);

        return new ApiResult<bool>(true, true, "File moved successfully.");
    }
}


public static class MoveFileEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/file/move",
            async (MoveFileRequest moveRequest, MoveFileRequestValidator validator, MoveFileHandler handler, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(moveRequest, cancellationToken);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errors, false, "Validation failed."));
                }

                var handle = await handler.Handle(moveRequest, cancellationToken);
                
                return handle.Success
                    ? Results.Ok(handle)
                    : Results.BadRequest(handle);
            }).RequireAuthorization();
    }
}