using FileService.Services;
using FluentValidation;
using MassTransit;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.File;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Features;

public record UploadFileRequest(IFormFile File, int WorkspaceId, string FolderPath, int UploadedBy);

public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    public UploadFileRequestValidator()
    {
        RuleFor(x => x.File).NotNull().WithMessage("File is required");
        
        RuleFor(x => x.FolderPath)
            .NotNull().WithMessage("Folder path is required")
            .MinimumLength(0); // 0 means root folder is allowed ("") 

        RuleFor(x => x.WorkspaceId)
            .GreaterThan(0).WithMessage("Workspace ID must be valid.");

        RuleFor(x => x.UploadedBy)
            .GreaterThan(0).WithMessage("UploadedBy must be valid.");
    }
}


public class UploadFileHandler
{
    private readonly BlobStorageService _blobStorageService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly MSHttpClient _httpClient;

    public UploadFileHandler(
        BlobStorageService blobStorageService, 
        IPublishEndpoint publishEndpoint,
        MSHttpClient httpClient)
    {
        _blobStorageService = blobStorageService;
        _publishEndpoint = publishEndpoint;
        _httpClient = httpClient;
    }

    public async Task<IResult> Handle(UploadFileRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Check file size
        if (request.File.Length > 500 * 1024 * 1024) // 500 MB limit
        {
            return Results.BadRequest(new ApiResult<object>(null, false, "File size exceeds the limit of 10 MB."));
        }

        var url = MicroserviceEndpoints.WorkspaceService.IsUserInWorkspace(request.UploadedBy, request.WorkspaceId);
        var  response = await _httpClient.GetAsync<bool>(url, cancellationToken);
        if (!response.Success)
        {
            return Results.BadRequest(new ApiResult<object>(response, false, "Failed to check user in workspace."));
        }
        
        if (!response.Success)
        {
            return Results.BadRequest(new ApiResult<object>(null, false, "User is not in workspace."));
        }

        var fileId = Guid.NewGuid();

        var result = await _blobStorageService.UploadFileAsync(
            request.File,
            request.FolderPath,
            fileId,
            cancellationToken);

        if (string.IsNullOrEmpty(result.url))
        {
            return Results.BadRequest(new ApiResult<object>(null, false, "Failed to upload file."));
        }

        var fileUploadedEvent = new FileUploadedEvent
        {
            FileId = fileId.ToString(),
            FileName = result.fileName,
            WorkspaceId = request.WorkspaceId,
            FolderPath = request.FolderPath,
            ContentType = request.File.ContentType ?? "application/octet-stream",
            Size = request.File.Length,
            UploadedAt = DateTime.UtcNow,
            UploadedBy = request.UploadedBy
        };

        await _publishEndpoint.Publish(fileUploadedEvent);

        return Results.Ok(new ApiResult<string>(result.url, true, "File uploaded successfully"));
    }
}

[IgnoreAntiforgeryToken]
public static class UploadFileEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/file/upload",
            async (HttpRequest request, UploadFileHandler handler, CancellationToken cancellationToken) =>
            {
                if (!request.HasFormContentType || !request.Form.Files.Any())
                {
                    return Results.BadRequest(new ApiResult<object>(null, false, "No file was uploaded."));
                }

                var userId = request.HttpContext.User.FindFirst("id")?.Value;
                var file = request.Form.Files.First();
                var workspaceIdStr = request.Form["workspaceId"].ToString();
                var folderPath = request.Form["folderPath"].ToString();

                Console.WriteLine($"UserId: {userId}");
                Console.WriteLine($"WorkspaceId: {workspaceIdStr}");
                
                if (!int.TryParse(workspaceIdStr, out var workspaceId))
                {
                    return Results.BadRequest(new ApiResult<object>(null, false, "Invalid WorkspaceId."));
                }

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Results.BadRequest(new ApiResult<object>(null, false, "User ID is required."));
                }
                var uploadRequest = new UploadFileRequest(file, workspaceId, folderPath, int.Parse(userId));

                var validator = new UploadFileRequestValidator();
                var validationResult = await validator.ValidateAsync(uploadRequest, cancellationToken);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, false, "Validation failed"));
                }

                return await handler.Handle(uploadRequest, cancellationToken);
            }).RequireAuthorization()
            ;
    }
}