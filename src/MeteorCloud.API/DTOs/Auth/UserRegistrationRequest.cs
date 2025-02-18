namespace MeteorCloud.API.DTOs.Auth;

public class UserRegistrationRequest
{
    public string FirstName { get; set; }
    
    public string LastName { get; set; }
    
    public string Email { get; set; }
    
    public string Password { get; set; }
}
