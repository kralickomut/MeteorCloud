namespace MeteorCloud.API.DTOs.User;

public class UpdateUserRequest
{
    public int Id { get; set; }
    public string Email { get; set; }
    
    public string FirstName { get; set; }
    
    public string LastName { get; set; }
}