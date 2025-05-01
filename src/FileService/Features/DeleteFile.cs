using System.ComponentModel.DataAnnotations;
using FileService.Services;
using FluentValidation;
using MassTransit;
using MeteorCloud.Messaging.Events.File;
using MeteorCloud.Shared.ApiResults;

namespace FileService.Features;

public record DeleteFileRequest(string Path, int DeletedBy);

public class DeleteFileRequestValidator : AbstractValidator<DeleteFileRequest>
{
    public DeleteFileRequestValidator()
    {
        RuleFor(x => x.Path)
            .NotEmpty().WithMessage("Path is required")
            .MinimumLength(2).WithMessage("Path must be at least 2 characters long");
    }
}

public class DeleteFileHandler
{
    private readonly BlobStorageService _blobStorageService;
    private readonly ILogger<DeleteFileHandler> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public DeleteFileHandler(BlobStorageService blobStorageService, ILogger<DeleteFileHandler> logger,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _blobStorageService = blobStorageService;
    }
    

    public async Task<ApiResult<bool>> Handle(DeleteFileRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var segments = request.Path.Split('/');

        if (segments.Length < 2 || !int.TryParse(segments[0], out var workspaceId))
        {
            _logger.LogWarning("Invalid path format, cannot extract WorkspaceId from: {Path}", request.Path);
            return new ApiResult<bool>(false, false, "Invalid path format");
        }

        var (result, fileSizeBytes, fileName) = await _blobStorageService.DeleteFileAsync(request.Path, cancellationToken);

        if (result)
        {
            await _publishEndpoint.Publish(new FileDeletedEvent
            {
                FileId = request.Path.Split("/").Last(),
                WorkspaceId = workspaceId,
                Size = fileSizeBytes ?? 0, 
                DeletedBy = request.DeletedBy,
                FileName = fileName ?? "Unknown"
            });
        }

        return result
            ? new ApiResult<bool>(true)
            : new ApiResult<bool>(false, false, "Failed to delete file");
    }
}

public static class DeleteFileEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/file/delete/{**path}",
            async (string path,
                HttpContext httpContext,
                DeleteFileHandler handler, 
                DeleteFileRequestValidator validator, 
                CancellationToken cancellationToken) =>
            {

                var userIdClaim = httpContext.User.FindFirst("id");

                if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Unauthorized(); 
                }
                
                var request = new DeleteFileRequest(path, userId);
                var validationResult = await validator.ValidateAsync(request, cancellationToken);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, false, "Validation failed"));
                }
                
                var result = await handler.Handle(request, cancellationToken);

                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            }).RequireAuthorization();
    }
}