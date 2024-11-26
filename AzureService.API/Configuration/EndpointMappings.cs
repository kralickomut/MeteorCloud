using AzureService.Application.Abstraction;

namespace AzureService.API.Configuration;

public static class EndpointMappings
{
    
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/uploadfile", async (IBlobStorageService _blobStorageService) =>
        {
            await _blobStorageService.UploadFileAsync("container1", "file1", new MemoryStream());
            return Results.Ok("File uploaded successfully."); // Return a proper response
        });
        
        app.MapGet("/", () =>
            Results.Ok("AzureService.API is running.")
        );
    }
}