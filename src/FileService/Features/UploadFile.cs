using FileService.Services;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Features;

public record UploadFileRequest(IFormFile File, string Path);

public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    public UploadFileRequestValidator()
    {
        RuleFor(x => x.File).NotNull().WithMessage("File is required");
        RuleFor(x => x.Path)
            .NotEmpty().WithMessage("Path is required")
            .MinimumLength(2).WithMessage("Path must be at least 2 characters long");

    }
}


public class UploadFileHandler
{
    private readonly BlobStorageService _blobStorageService;
    
    public UploadFileHandler(BlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }
    
    public async Task<ApiResult<string>> Handle(UploadFileRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var fileUrl = await _blobStorageService.UploadFileAsync(request.File, request.Path, cancellationToken);
        
        return string.IsNullOrEmpty(fileUrl) 
            ? new ApiResult<string>(null, false, "Failed to upload file")
            : new ApiResult<string>(fileUrl);
    }
}

[IgnoreAntiforgeryToken]
public static class UploadFileEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/file/upload/{workspaceId}",
            async (string workspaceId, HttpRequest request, UploadFileHandler handler, CancellationToken cancellationToken) =>
            {
                if (!request.HasFormContentType || !request.Form.Files.Any())
                {
                    return Results.BadRequest(new ApiResult<object>(null, false, "No file was uploaded."));
                }

                var file = request.Form.Files.First();

                var filePath = $"{workspaceId}";
                var fileRequest = new UploadFileRequest(file, filePath);
                var validationResult = await new UploadFileRequestValidator().ValidateAsync(fileRequest, cancellationToken);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, false, "Validation failed"));
                }

                var result = await handler.Handle(fileRequest, cancellationToken);

                if (!result.Success)
                {
                    return Results.BadRequest(new ApiResult<object>(null, false, "File upload failed"));
                }

                return Results.Ok(new ApiResult<string>(result.Data, true, "File uploaded successfully"));
            })
        .Accepts<UploadFileRequest>("multipart/form-data");
    }
}