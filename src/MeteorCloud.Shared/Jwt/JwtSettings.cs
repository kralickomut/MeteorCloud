namespace MeteorCloud.Shared.Jwt;

public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string PublicKeyPath { get; set; } = string.Empty;
}