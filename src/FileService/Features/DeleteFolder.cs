using FileService.Services;
using FluentValidation;
using MassTransit;
using MeteorCloud.Messaging.Events.File;
using MeteorCloud.Shared.ApiResults;

namespace FileService.Features;

public record DeleteFolderRequest(string Path);

public class DeleteFolderRequestValidator : AbstractValidator<DeleteFolderRequest>
{
    public DeleteFolderRequestValidator()
    {
        RuleFor(x => x.Path)
            .NotEmpty()
            .WithMessage("Path cannot be empty.");
    }
}

public class DeleteFolderHandler
{
    
    private readonly BlobStorageService _blobStorageService;
    private readonly IPublishEndpoint _publishEndpoint;
    
    public DeleteFolderHandler(BlobStorageService blobStorageService, IPublishEndpoint publishEndpoint)
    {
        _blobStorageService = blobStorageService;
        _publishEndpoint = publishEndpoint;
    }
    
    public async Task<ApiResult<bool>> Handle(DeleteFolderRequest request, CancellationToken cancellationToken)
    {
        var path = request.Path;

        // Delete the folder from blob storage
        var result = await _blobStorageService.DeleteFolderAsync(path, cancellationToken);

        if (!result)
        {
            return new ApiResult<bool>(false, false, "Failed to delete folder.");
        }
        
        await _publishEndpoint.Publish(new FolderDeletedEvent
        {
            Path = path
        });
        
        return new ApiResult<bool>(true, true, "Folder deleted successfully.");
    }
}

public class DeleteFolderEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/file/delete-folder/{**path}",
            async (string path, DeleteFolderHandler handler, DeleteFolderRequestValidator validator, CancellationToken cancellationToken) =>
            {
                var request = new DeleteFolderRequest(path);
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