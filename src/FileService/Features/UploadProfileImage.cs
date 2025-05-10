using System.Threading;
using System.Threading.Tasks;
using FileService.Services;
using FluentValidation;
using MassTransit;
using MeteorCloud.Messaging.Events;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.Http;

namespace FileService.Features;

public record UploadProfileImageRequest(IFormFile File, int UserId);

public class UploadProfileImageValidator : AbstractValidator<UploadProfileImageRequest>
{
    public UploadProfileImageValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("Profile image is required");

        RuleFor(x => x.File.Length)
            .LessThanOrEqualTo(5 * 1024 * 1024).WithMessage("Image must be under 5MB");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId must be valid.");
    }
}


public class UploadProfileImageHandler
{
    private readonly BlobStorageService _blobStorageService;
    private readonly IPublishEndpoint _publishEndpoint;

    public UploadProfileImageHandler(BlobStorageService blobStorageService, IPublishEndpoint publishEndpoint)
    {
        _blobStorageService = blobStorageService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<IResult> Handle(UploadProfileImageRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var result = await _blobStorageService.UploadProfileImageAsync(
            request.File,
            request.UserId,
            cancellationToken);

        if (string.IsNullOrEmpty(result))
        {
            return Results.BadRequest(new ApiResult<object>(null, false, "Failed to upload profile image."));
        }
        
        await _publishEndpoint.Publish(new ProfileImageUploadedEvent
        {
            UserId = request.UserId,
            ImageUrl = result
        }, cancellationToken);

        return Results.Ok(new ApiResult<string>(result, true, "Profile image uploaded successfully"));
    }
}


public static class UploadProfileImageEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/file/profile-image",
            async (HttpRequest request, UploadProfileImageHandler handler, UploadProfileImageValidator validator, CancellationToken cancellationToken) =>
            {
                if (!request.HasFormContentType || !request.Form.Files.Any())
                {
                    return Results.BadRequest(new ApiResult<object>(null, false, "No image was uploaded."));
                }

                var userId = request.HttpContext.User.FindFirst("id")?.Value;
                var file = request.Form.Files.First();

                if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(userId, out var uid))
                {
                    return Results.BadRequest(new ApiResult<object>(null, false, "Invalid or missing UserId."));
                }

                var uploadRequest = new UploadProfileImageRequest(file, uid);
                var validationResult = await validator.ValidateAsync(uploadRequest, cancellationToken);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, false, "Validation failed"));
                }
                
                return await handler.Handle(uploadRequest, cancellationToken);
            }).RequireAuthorization();
    }
}