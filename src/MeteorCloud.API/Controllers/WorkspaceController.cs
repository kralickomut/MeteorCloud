using MeteorCloud.API.DTOs.Workspace;
using MeteorCloud.Communication;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeteorCloud.API.Controllers;

[ApiController]
[Authorize]
[Route("api/workspace")]
public class WorkspaceController : ControllerBase
{
    private readonly MSHttpClient _httpClient;
    
    public WorkspaceController(MSHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [HttpPost]
    public async Task<IActionResult> Create(WorkspaceCreateRequest request, CancellationToken cancellationToken)
    {
        var url = MicroserviceEndpoints.WorkspaceService + $"/api/workspace";

        var response = await _httpClient.PostAsync<WorkspaceCreateRequest, object>(url, request, cancellationToken);
        
        if (response.Success is false)
        {
            return BadRequest(new ApiResult<object>(null, false, response.Message ?? "Workspace creation failed"));
        }
        
        return Ok(new ApiResult<object>(response.Data, true, "Workspace created successfully"));
    }
    
    [HttpDelete]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var url = MicroserviceEndpoints.WorkspaceService + $"/api/workspace/{id}";

        var response = await _httpClient.DeleteAsync<object>(url, cancellationToken);
        
        if (response.Success is false)
        {
            return BadRequest(new ApiResult<object>(null, false, response.Message ?? "Workspace deletion failed"));
        }
        
        return Ok(new ApiResult<object>(null, true, "Workspace deleted successfully"));
    }
}