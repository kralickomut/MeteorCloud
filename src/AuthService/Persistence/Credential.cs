namespace AuthService.Persistence;

public class Credential
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string? VerificationCode { get; set; }
    public DateTime? VerificationExpiry { get; set; }
    public DateTime CreatedAt { get; set; }
}