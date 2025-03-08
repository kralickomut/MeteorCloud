using System.ComponentModel.DataAnnotations;
using FileService.Services;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;

namespace FileService.Features;

public record DeleteFileRequest(string Path);

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

    public DeleteFileHandler(BlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    public async Task<ApiResult<object>> Handle(DeleteFileRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _blobStorageService.DeleteFileAsync(request.Path, cancellationToken);
        
        return result
            ? new ApiResult<object>(null)
            : new ApiResult<object>(null, false, "Failed to delete file");
    }
}

public static class DeleteFileEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/file/delete/{**path}",
            async (string path, DeleteFileHandler handler, DeleteFileRequestValidator validator, CancellationToken cancellationToken) =>
            {
                var request = new DeleteFileRequest(path);
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
            });
    }
}