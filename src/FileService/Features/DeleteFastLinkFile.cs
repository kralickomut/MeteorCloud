using FileService.Services;
using FluentValidation;
using MassTransit;
using MeteorCloud.Messaging.Events.FastLink;
using MeteorCloud.Shared.ApiResults;

namespace FileService.Features;

public record DeleteFastLinkFileRequest(string Path, int DeletedBy);

public class DeleteFastLinkFileValidator : AbstractValidator<DeleteFastLinkFileRequest>
{
    public DeleteFastLinkFileValidator()
    {
        RuleFor(x => x.Path)
            .NotEmpty()
            .WithMessage("Path cannot be empty");
        
        RuleFor(x => x.DeletedBy)
            .NotEmpty()
            .WithMessage("User ID cannot be empty")
            .GreaterThan(0)
            .WithMessage("User ID must be greater than 0");
    }
}

public class DeleteFastLinkFileHandler
{
    private readonly BlobStorageService _blobStorageService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteFastLinkFileHandler> _logger;

    public DeleteFastLinkFileHandler(BlobStorageService blobStorageService, IPublishEndpoint publishEndpoint, ILogger<DeleteFastLinkFileHandler> logger)
    {
        _blobStorageService = blobStorageService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<ApiResult<bool>> Handle(DeleteFastLinkFileRequest request, CancellationToken cancellationToken)
    {
        var (success, fileSize, fileName) = await _blobStorageService.DeleteFileAsync(request.Path, cancellationToken);

        if (!success)
            return new ApiResult<bool>(false, false, "Could not delete file");

        await _publishEndpoint.Publish(new FastLinkFileDeletedEvent
        {
            FileId = Guid.Parse(request.Path.Split("/").Last()),
            UserId = request.DeletedBy
        });

        return new ApiResult<bool>(true);
    }
}


public static class DeleteFastLinkFileEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/file/fast-link/delete/{**path}",
            async (string path,
                HttpContext httpContext,
                DeleteFastLinkFileHandler handler,
                CancellationToken cancellationToken) =>
            {
                var userIdClaim = httpContext.User.FindFirst("id");
                if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
                    return Results.Unauthorized();

                var request = new DeleteFastLinkFileRequest(path, userId);
                return await handler.Handle(request, cancellationToken) switch
                {
                    { Success: true } result => Results.Ok(result),
                    var result => Results.BadRequest(result)
                };
            }).RequireAuthorization();
    }
}