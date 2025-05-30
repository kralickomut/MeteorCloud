using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using MeteorCloud.Shared.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services;

public class TokenService
{
    private readonly RsaSecurityKey _key;
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
        _key = RsaKeyUtils.LoadEmbeddedPrivateKey("AuthService.Keys.private.pem");
    }

    public string GenerateToken(int userId, string email)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>
        {
            new Claim("id", userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(60),
            Issuer = _config["JwtSettings:Issuer"],
            Audience = _config["JwtSettings:Audience"],
            SigningCredentials = credentials
        };

        return handler.WriteToken(handler.CreateToken(tokenDescriptor));
    }
}