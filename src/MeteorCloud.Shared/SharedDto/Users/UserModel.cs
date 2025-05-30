namespace MeteorCloud.Shared.SharedDto.Users;

public class UserModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public int InTotalWorkspaces { get; set; } = 0;
    public string? ProfilePictureUrl { get; set; } = null;

    public DateTime? LastLogin { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } 
}

public class UserResponse
{
    public UserModel User { get; set; }
}