using MeteorCloud.API.DTOs.Auth;
using MeteorCloud.Communication;
using Microsoft.AspNetCore.Mvc;

namespace MeteorCloud.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly MSHttpClient _httpClient;
    
    public AuthController(MSHttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationRequest request)
    {
        var userUrl = MicroserviceEndpoints.UserService + "/api/users";
        var userResponse = await _httpClient.PostAsync<UserRegistrationRequest, object>(userUrl, request);
        
        if (userResponse.Success is false)
        {
            return BadRequest(userResponse);
        }

        Console.WriteLine("<----------- User registered here --------->");

        var authUrl = MicroserviceEndpoints.AuthService + "/api/credentials";
        var authResponse = await _httpClient.PostAsync<object, object>(authUrl, 
            new { Email = request.Email, Password = request.Password, UserId = userResponse.Data });

        if (authResponse.Success is false)
        {
            return BadRequest();
        }
        
        
        return Ok("Registration successful");
    }
}