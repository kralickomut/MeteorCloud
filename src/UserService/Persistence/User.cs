using System;

namespace UserService.Persistence;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    public string? Description { get; set; } = string.Empty;

    public int InTotalWorkspaces { get; set; } = 0;
    
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; } 
}