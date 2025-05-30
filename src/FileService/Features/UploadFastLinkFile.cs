using FileService.Services;
using FluentValidation;
using MassTransit;
using MeteorCloud.Messaging.Events.FastLink;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Features;

public record UploadFastLinkFileRequest(IFormFile File, int UserId, string LinkName, int ExpiresInHours = 24);

public class UploadFastLinkFileValidator : AbstractValidator<UploadFastLinkFileRequest>
{
    public UploadFastLinkFileValidator()
    {
        RuleFor(x => x.File).NotNull().WithMessage("File is required");
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("User ID must be greater than 0");
        RuleFor(x => x.ExpiresInHours)
            .GreaterThan(0)
            .LessThanOrEqualTo(24)
            .WithMessage("Expiration time must be between 1 and 24 hours");
        RuleFor(x => x.LinkName)
            .NotNull()
            .WithMessage("Link name is required");
    }
}

public class UploadFastLinkFileHandler
{
    private readonly BlobStorageService _blobStorageService;
    private readonly IPublishEndpoint _publishEndpoint;

    public UploadFastLinkFileHandler(BlobStorageService blobStorageService, IPublishEndpoint publishEndpoint)
    {
        _blobStorageService = blobStorageService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<IResult> Handle(UploadFastLinkFileRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.File.Length > 500 * 1024 * 1024) // 500MB limit
        {
            return Results.BadRequest(new ApiResult<object>(null, false, "File size exceeds the 500MB limit."));
        }

        var fileId = Guid.NewGuid();

        var fileUrl = await _blobStorageService.UploadFastLinkFileAsync(
            request.File,
            request.UserId,
            fileId,
            cancellationToken);

        await _publishEndpoint.Publish(new FastLinkFileUploadedEvent()
        {
            FileId = fileId,
            UserId = request.UserId,
            FileName = request.File.FileName,
            ExpiresAt = DateTime.UtcNow.AddHours(request.ExpiresInHours),
            FileSize = request.File.Length,
            Name = request.LinkName,
            Token = Guid.NewGuid()
        });

        if (string.IsNullOrEmpty(fileUrl))
        {
            return Results.BadRequest(new ApiResult<object>(null, false, "Failed to upload file."));
        }

        return Results.Ok(new ApiResult<string>(fileUrl, true, "Fast link file uploaded successfully."));
    }
}

[IgnoreAntiforgeryToken]
public static class UploadFastLinkFileEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/file/fast-link/upload", async (
            HttpRequest request,
            UploadFastLinkFileHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType || !request.Form.Files.Any())
            {
                return Results.BadRequest(new ApiResult<object>(null, false, "No file was uploaded."));
            }

            var userId = request.HttpContext.User.FindFirst("id")?.Value;
            var file = request.Form.Files.First();
            var linkName = request.Form["linkName"].ToString();
            
            if (!int.TryParse(request.Form["expiresInHours"], out var expiresInHours))
            {
                expiresInHours = 24;
            }
            
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.BadRequest(new ApiResult<object>(null, false, "User ID is required."));
            }

            var uploadRequest = new UploadFastLinkFileRequest(file, int.Parse(userId), linkName, expiresInHours);

            var validator = new UploadFastLinkFileValidator();
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