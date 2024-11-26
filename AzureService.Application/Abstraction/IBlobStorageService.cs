namespace AzureService.Application.Abstraction;

public interface IBlobStorageService
{
    // Uploads a file to the blob storage
    Task<string> UploadFileAsync(string containerName, string fileName, Stream fileStream);
    
    // Downloads a file from the blob storage
    Task<Stream> DownloadFileAsync(string containerName, string fileName);
    
    // Deletes a file from the blob storage
    Task DeleteFileAsync(string containerName, string fileName);
    
}