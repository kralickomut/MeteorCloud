namespace MeteorCloud.API.DTOs.Auth;

public class UserLoginRequest
{
    public string Email { get; set; }
    
    public string Password { get; set; }
}