using System;
using MeteorCloud.Shared.SharedDto.Users;

namespace UserService.Persistence;

public class User
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
    
    
    public UserModel ToModel()
    {
        return new UserModel
        {
            Id = Id,
            Name = Name,
            Email = Email,
            Description = Description,
            InTotalWorkspaces = InTotalWorkspaces,
            ProfilePictureUrl = ProfilePictureUrl,
            LastLogin = LastLogin,
            RegistrationDate = RegistrationDate,
            UpdatedAt = UpdatedAt
        };
    } 
}