namespace AuthService.Persistence;

public class Credential
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public string Email { get; set; }
    
    public string PasswordHash { get; set; }
}