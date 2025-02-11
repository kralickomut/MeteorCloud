namespace UserService.Application.TransferObjects;

public class UserRegistrationModel
{
    public string Username { get; set; }
    
    public string Email { get; set; }
    public string Password { get; set; }
    public string PasswordAgain { get; set; }
}