using Azure.Storage.Blobs;
using AzureService.Application.Abstraction;
using Messaging.Base.Abstraction;
using Messaging.Base.Events;

namespace AzureService.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    //private readonly BlobServiceClient _blobServiceClient;
    private readonly IEventBus _eventBus;
    
    public BlobStorageService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public async Task<string> UploadFileAsync(string containerName, string fileName, Stream fileStream)
    {
        // var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        // var blobClient = containerClient.GetBlobClient(fileName);
        // await blobClient.UploadAsync(fileStream);
        
        await _eventBus.PublishAsync(new FileUploadedEvent(fileName, fileStream.Length));
        return $"File: \"{fileName}\" uploaded from BlobStorageService";
    }

    public async Task<Stream> DownloadFileAsync(string containerName, string fileName)
    {
        // var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        // var blobClient = containerClient.GetBlobClient(fileName);
        // var response = await blobClient.DownloadAsync();
        
        //return response.Value.Content;
        throw new NotImplementedException();
    }

    public async Task DeleteFileAsync(string containerName, string fileName)
    {
        // var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        // var blobClient = containerClient.GetBlobClient(fileName);
        // await blobClient.DeleteAsync();

        throw new NotImplementedException();
    }
}