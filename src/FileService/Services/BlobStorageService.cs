using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileService.Models;

namespace FileService.Services;

public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(BlobServiceClient blobServiceClient, IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = configuration["AzureBlobStorage:ContainerName"]!;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folderPath, Guid fileId, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobPath = string.IsNullOrWhiteSpace(folderPath)
            ? fileId.ToString()
            : $"{folderPath}/{fileId}";

        var blobClient = containerClient.GetBlobClient(blobPath);
        
        // Make sure the blob name is safe for Azure Storage
        var safeName = SanitizeFileName(file.FileName);

        var metadata = new Dictionary<string, string>
        {
            { "originalfilename", safeName } //  Save original filename into metadata
        };

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType },
                Metadata = metadata
            },
            cancellationToken: cancellationToken
        );

        return blobClient.Uri.ToString();
    }
    
    public async Task<(bool Success, long? FileSizeBytes, string? FileName)> DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"Attempting to delete file at path: {path}");

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(path);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning($"Blob does not exist: {blobClient.Uri}");
                return (false, null, null);
            }

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var size = properties.Value.ContentLength;

            // Extract original file name from metadata
            properties.Value.Metadata.TryGetValue("originalfilename", out var originalFileName);

            var response = await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                cancellationToken: cancellationToken);

            _logger.LogInformation(response
                ? $"File successfully deleted: {blobClient.Uri}"
                : $"File deletion returned false for: {blobClient.Uri}");

            return (response, size, originalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting blob: {ex.Message}");
            return (false, null, null);
        }
    }
    
    public async Task<bool> DeleteFolderAsync(string folderPrefix, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"Attempting to delete all blobs under: {folderPrefix}");

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            if (containerClient == null)
            {
                _logger.LogError("Blob container client is null. Check connection string.");
                return false;
            }

            // Check if the container exists
            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning($"Container '{_containerName}' does not exist. Nothing to delete.");
                return true; // Treat as success, nothing needs to be deleted
            }

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: folderPrefix, cancellationToken: cancellationToken))
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                _logger.LogInformation($"Deleting blob: {blobClient.Name}");

                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
            }

            _logger.LogInformation($"All blobs under '{folderPrefix}' deleted successfully.");
            return true;
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError($"Azure Storage request failed: {ex.Message} | Error Code: {ex.ErrorCode}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error while deleting folder: {ex.Message}");
            return false;
        }
    }
    
    public async Task<DownloadResult?> DownloadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(path);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning($"Blob does not exist: {blobClient.Uri}");
            return null;
        }

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

        var contentType = response.Value.Details.ContentType ?? "application/octet-stream";
    
        //  Try to fetch original file name from metadata
        var fileName = properties.Value.Metadata.TryGetValue("originalfilename", out var originalFileName)
            ? originalFileName
            : path.Split('/').Last(); // fallback if not present

        return new DownloadResult()
        {
            ContentType = contentType,
            FileName = fileName,
            Stream = response.Value.Content
        };
    }
    
    private string SanitizeFileName(string name)
    {
        return string.Concat(name.Normalize(NormalizationForm.FormD)
                .Where(c => char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark))
            .Replace(" ", "_");
    }
}