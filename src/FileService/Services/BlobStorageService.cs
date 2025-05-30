using System.Text;
using System.Text.RegularExpressions;
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

    public async Task<(string url, string fileName)> UploadFileAsync(IFormFile file, string folderPath, Guid fileId, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var safeName = SanitizeFileName(file.FileName);
        var finalDisplayName = await GetUniqueFilenameAsync(containerClient, folderPath, safeName, cancellationToken);

        var blobPath = string.IsNullOrWhiteSpace(folderPath)
            ? fileId.ToString()
            : $"{folderPath}/{fileId}";

        var blobClient = containerClient.GetBlobClient(blobPath);

        var metadata = new Dictionary<string, string>
        {
            { "originalfilename", finalDisplayName }
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

        return (blobClient.Uri.ToString(), finalDisplayName);
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
    
    
    public async Task<string> UploadProfileImageAsync(IFormFile file, int userId, CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var safeFileName = SanitizeFileName(file.FileName);
        var blobPath = $"profile-images/{userId}/profile.jpg";
        var blobClient = containerClient.GetBlobClient(blobPath);

        var metadata = new Dictionary<string, string>
        {
            { "originalfilename", safeFileName }
        };

        await using var stream = file.OpenReadStream();

        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType ?? "image/jpeg" },
                Metadata = metadata,
                Conditions = null
            },
            cancellationToken: cancellationToken
        );

        _logger.LogInformation("📸 Uploaded profile image for user {UserId} to {BlobPath}", userId, blobPath);

        return blobClient.Uri.ToString();
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
    
    public async Task<string> UploadFastLinkFileAsync(IFormFile file, int userId, Guid fileId, CancellationToken cancellationToken)
    {
        var folderPath = $"fast-link-files/{userId}";

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobPath = $"{folderPath}/{fileId}";
        var blobClient = containerClient.GetBlobClient(blobPath);

        var safeName = SanitizeFileName(file.FileName);

        var metadata = new Dictionary<string, string>
        {
            { "originalfilename", safeName } // Just store the original name without suffixing
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
    
    
    public async Task<string> MoveFileAsync(string sourcePath, string destinationFolder, CancellationToken cancellationToken = default)
{
    var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

    var sourceBlob = containerClient.GetBlobClient(sourcePath);

    if (!await sourceBlob.ExistsAsync(cancellationToken))
    {
        _logger.LogWarning($"Source blob not found: {sourcePath}");
        throw new FileNotFoundException("Source file does not exist.");
    }

    var sourceProperties = await sourceBlob.GetPropertiesAsync(cancellationToken: cancellationToken);

    sourceProperties.Value.Metadata.TryGetValue("originalfilename", out var originalName);
    originalName ??= Path.GetFileName(sourcePath); // fallback

    // Step 1: Resolve name collision
    var finalName = await GetUniqueFilenameAsync(containerClient, destinationFolder, originalName, cancellationToken);

    // Step 2: Compose destination path
    var fileGuid = Path.GetFileName(sourcePath); // We keep the same GUID-based blob name
    var newPath = string.IsNullOrWhiteSpace(destinationFolder)
        ? fileGuid
        : $"{destinationFolder}/{fileGuid}";

    var destBlob = containerClient.GetBlobClient(newPath);

    // Step 3: Copy and update metadata
    await destBlob.StartCopyFromUriAsync(sourceBlob.Uri, cancellationToken: cancellationToken);
    await destBlob.SetMetadataAsync(new Dictionary<string, string> {
        { "originalfilename", finalName }
    }, cancellationToken: cancellationToken);

    // Step 4: Delete the old blob
    await sourceBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

    _logger.LogInformation($"Moved blob from {sourcePath} to {newPath} with name {finalName}");

    return newPath;
}
    
    
    

private async Task<string> GetUniqueFilenameAsync(BlobContainerClient containerClient, string folderPath, string originalName, CancellationToken cancellationToken)
{
    string prefix = string.IsNullOrWhiteSpace(folderPath) ? "" : $"{folderPath}/";
    var existingNames = new List<string>();

    await foreach (var blob in containerClient.GetBlobsAsync(BlobTraits.Metadata, prefix: prefix, cancellationToken: cancellationToken))
    {
        if (blob.Metadata != null && blob.Metadata.TryGetValue("originalfilename", out var name))
        {
            existingNames.Add(name);
        }
    }

    return ResolveFilenameConflict(existingNames, originalName);
}

private string ResolveFilenameConflict(IEnumerable<string> existingNames, string originalName)
{
    var nameSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
    var baseName = Path.GetFileNameWithoutExtension(originalName);
    var extension = Path.GetExtension(originalName);
    var candidate = originalName;
    int counter = 1;

    while (nameSet.Contains(candidate))
    {
        candidate = $"{baseName} ({counter}){extension}";
        counter++;
    }

    return candidate;
}
    
private string SanitizeFileName(string name)
{
    var normalized = name.Normalize(NormalizationForm.FormD);

    var sanitized = new string(normalized
        .Where(c => char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
        .ToArray());

    // Remove or replace unsafe characters (keep only ASCII letters, digits, dash, underscore, dot)
    sanitized = Regex.Replace(sanitized, @"[^a-zA-Z0-9_\-\.]", "_");

    return sanitized;
}
}