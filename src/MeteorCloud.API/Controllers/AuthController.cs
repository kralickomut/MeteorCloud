using FluentValidation;
using MeteorCloud.API.DTOs.Auth;
using MeteorCloud.Communication;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.Mvc;

namespace MeteorCloud.API.Controllers;


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly MSHttpClient _httpClient;
    private readonly IValidator<UserRegistrationRequest> _registrationValidator;
    private readonly IValidator<UserLoginRequest> _loginValidator;
    
    public AuthController(
        MSHttpClient httpClient, 
        IValidator<UserRegistrationRequest> registrationValidator,
        IValidator<UserLoginRequest> loginValidator)
    {
        _httpClient = httpClient;
        _registrationValidator = registrationValidator;
        _loginValidator = loginValidator;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationRequest request)
    {
        var validationResult = await _registrationValidator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage);
            return BadRequest(new ApiResult<object>(errorMessages, false, "Validation Failed"));
        }
        
        var userUrl = MicroserviceEndpoints.UserService + "/api/users";
        //var userUrl = "http://localhost:5295/api/users";
        var userResponse = await _httpClient.PostAsync<UserRegistrationRequest, object>(userUrl, request);
        
        if (userResponse.Success is false)
        {
            return BadRequest(new ApiResult<object>(null, false, userResponse.Message ?? "User registration failed"));
        }
        
        var authUrl = MicroserviceEndpoints.AuthService + "/api/credentials";
        //var authUrl = "http://localhost:5296/api/credentials";
        var authResponse = await _httpClient.PostAsync<object, object>(authUrl, 
            new { Email = request.Email, Password = request.Password, UserId = userResponse.Data });

        if (authResponse.Success is false)
        {
            return BadRequest();
        }
        
        
        return Ok(new ApiResult<object>(default, true, "User registered successfully"));
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        var validationResult = await _loginValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage);
            return BadRequest(new ApiResult<object>(errorMessages, false, "Validation Failed"));
        }
        
        var authUrl = MicroserviceEndpoints.AuthService + "/api/auth/login";
        //var authUrl = "http://localhost:5296/api/auth/login";
        var authResponse = await _httpClient.PostAsync<UserLoginRequest, object>(authUrl, request);
        
        if (authResponse.Success is false)
        {
            return BadRequest(new ApiResult<object>(null, false, authResponse.Message ?? "Login failed"));
        }
        
        return Ok(new ApiResult<object>(authResponse.Data, true, "Login successful"));
    }
}