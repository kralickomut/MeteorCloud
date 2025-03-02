using MeteorCloud.API.DTOs.User;
using MeteorCloud.Communication;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeteorCloud.API.Controllers;


[ApiController]
[Authorize]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly MSHttpClient _httpClient;
    
    public UserController(MSHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detail(int id)
    {
        
        var url = MicroserviceEndpoints.UserService + $"/api/users/{id}";
        //var url = "http://localhost:5295/api/users/" + id;
        
        var response = await _httpClient.GetAsync<object>(url);
        
        if (response.Success is false)
        {
            return BadRequest(new ApiResult<object>(null, false, response.Message ?? "User not found"));
        }
        
        return Ok(new ApiResult<object>(response.Data, true, "User found"));
    }
    
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var url = MicroserviceEndpoints.UserService + $"/api/users";
        //var url = "http://localhost:5295/api/users/" + id;
        
        var response = await _httpClient.PutAsync<UpdateUserRequest, object>(url, request, cancellationToken);
        
        if (response.Success is false)
        {
            return BadRequest(new ApiResult<object>(null, false, response.Message ?? "User update failed"));
        }
        
        return Ok(new ApiResult<object>(response.Data, true, "User updated successfully"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var url = MicroserviceEndpoints.UserService + $"/api/users/{id}";

        var response = await _httpClient.DeleteAsync<object>(url, cancellationToken);
        
        if (response.Success is false)
        {
            return BadRequest(new ApiResult<object>(null, false, response.Message ?? "User delete failed"));
        }
        
        return Ok(new ApiResult<object>(response.Data, true, "User deleted successfully"));
    }
    
}