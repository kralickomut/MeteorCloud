using System.Net.Http.Headers;
using MeteorCloud.API.DTOs.User;
using MeteorCloud.Communication;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeteorCloud.API.Controllers;


[ApiController]
[Authorize]
[Route("api/file")]
public class FileController : ControllerBase
{
    private readonly MSHttpClient _httpClient;
    
    public FileController(MSHttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file, 
        [FromForm] string path, // ✅ Path includes workspaceId
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ApiResult<object>(null, false, "No file was uploaded."));
        }

        var url = $"{MicroserviceEndpoints.FileService}/api/file/upload"; // ✅ No workspaceId
    
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        content.Add(fileContent, "file", file.FileName);
        content.Add(new StringContent(path), "path"); // ✅ Send full path including workspaceId

        var response = await _httpClient.PostAsync<string>(url, content, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(new ApiResult<object>(null, false, response.Message ?? "File upload failed"));
        }
    
        return Ok(new ApiResult<object>(response.Data, true, "File uploaded successfully"));
    }
    
    [HttpDelete("delete/{**path}")]
    public async Task<IActionResult> Delete(string path, CancellationToken cancellationToken)
    {
        var url = MicroserviceEndpoints.FileService + "/api/file/delete/" + path;

        var response = await _httpClient.DeleteAsync<object>(url, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(new ApiResult<object>(null, false, response.Message ?? "File deletion failed"));
        }

        return Ok(new ApiResult<object>(response.Data, true, "File deleted successfully"));
    }
}