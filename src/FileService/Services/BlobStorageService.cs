using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

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

    public async Task<string> UploadFileAsync(IFormFile file, int workspaceId, string folderPath, Guid fileId, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobPath = string.IsNullOrWhiteSpace(folderPath)
            ? $"{workspaceId}/{fileId}"
            : $"{workspaceId}/{folderPath}/{fileId}";

        var blobClient = containerClient.GetBlobClient(blobPath);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType }, cancellationToken: cancellationToken);

        return blobClient.Uri.ToString(); 
    }
    
    public async Task<bool> DeleteFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"🔍 Attempting to delete file at path: {path}");

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            if (containerClient == null)
            {
                _logger.LogError("❌ Blob container client is null. Check if the connection string is correct.");
                return false;
            }

            var blobClient = containerClient.GetBlobClient(path);
            _logger.LogInformation($"📝 BlobClient created with URI: {blobClient.Uri}");

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning($"⚠️ Blob does not exist: {blobClient.Uri}");
                return false;
            }

            var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
        
            if (response)
            {
                _logger.LogInformation($"✅ File successfully deleted: {blobClient.Uri}");
            }
            else
            {
                _logger.LogWarning($"⚠️ File deletion returned false for: {blobClient.Uri}");
            }

            return response;
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError($"🚨 Azure Storage request failed: {ex.Message} | Error Code: {ex.ErrorCode}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Unexpected error while deleting file: {ex.Message}");
            return false;
        }
    }
}