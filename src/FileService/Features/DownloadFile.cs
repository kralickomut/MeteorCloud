using FileService.Models;
using FileService.Services;

namespace FileService.Features;

public record DownloadFileRequest(string Path);

public class DownloadFileHandler
{
    private readonly BlobStorageService _blobStorageService;
    private readonly ILogger<DownloadFileHandler> _logger;

    public DownloadFileHandler(BlobStorageService blobStorageService, ILogger<DownloadFileHandler> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }
    
    public async Task<DownloadResult?> Handle(DownloadFileRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _blobStorageService.DownloadFileAsync(request.Path, cancellationToken);
    }
}


public static class DownloadFileEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/file/download/{**path}", async (
            string path,
            DownloadFileHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(path) || path.Length < 2)
            {
                return Results.BadRequest(new { Message = "Invalid path provided." });
            }
            
            var decodedPath = Uri.UnescapeDataString(path);

            var request = new DownloadFileRequest(decodedPath);
            var result = await handler.Handle(request, cancellationToken);

            if (result == null)
            {
                return Results.NotFound(new { Message = "File not found." });
            }

            return Results.File(
                fileStream: result.Stream,
                contentType: result.ContentType,
                fileDownloadName: result.FileName,
                enableRangeProcessing: true // 📂 optional but better for big files (allows resume)
            );
        });
    }
}