using AuthService.Persistence;
using MassTransit;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Messaging.Events;
using MeteorCloud.Messaging.Events.Auth;
using Newtonsoft.Json;

namespace AuthService.Services;

public class CredentialService
{
    private readonly CredentialRepository _credentialRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CredentialService> _logger;

    public CredentialService(CredentialRepository credentialRepository, IPublishEndpoint publishEndpoint, ILogger<CredentialService> logger)
    {
        _credentialRepository = credentialRepository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public async Task<Credential?> GetCredentialsByEmailAsync(string email)
    {
        var credential = await _credentialRepository.GetCredentialByEmail(email);

        return credential;
    }
    
    public async Task<Credential?> GetByUserId(int id)
    {
        var credential = await _credentialRepository.GetCredentialById(id);
        
        return credential;
    }
    
    public async Task<Credential?> GetCredentialsByResetPasswordTokenAsync(Guid token)
    {
        var credential = await _credentialRepository.GetCredentialByResetPasswordToken(token);
        
        return credential;
    }
    
    public async Task<Credential?> RegisterUserAsync(string email, string name, string password, CancellationToken cancellationToken)
    {
        var credential = new Credential
        {
            Email = email,
            PasswordHash = HashPassword(password),
            IsVerified = false,
            VerificationCode = GenerateVerificationCode(),
            VerificationExpiry = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        };
        
        var newCredential = await _credentialRepository.CreateCredential(credential, cancellationToken);
        
        if (newCredential is null)
        {
            throw new Exception("Failed to create credential. Email already in use.");
        }

        return newCredential;
    }

    public async Task<bool> Verify(string code, CancellationToken cancellationToken)
    {
        var credential = await _credentialRepository.UpdateVerificationStatusAsync(code, cancellationToken);
        
        if (credential == null)
        {
            return false;
        }

        // The verification logic if code expired is done in postgres
        return credential.IsVerified;
    }
    
    public async Task<bool> ResendVerificationCode(string email, CancellationToken cancellationToken)
    {
        var credential = await _credentialRepository.GetCredentialByEmail(email);
        
        if (credential == null || credential.IsVerified)
        {
            return false;
        }

        credential.VerificationCode = GenerateVerificationCode();
        credential.VerificationExpiry = DateTime.UtcNow.AddMinutes(30);

        await _credentialRepository.UpdateCredential(credential, cancellationToken);

        await _publishEndpoint.Publish(new VerificationCodeResentEvent(credential.Email, credential.VerificationCode));
        _logger.LogInformation("Resend verification code event published for user email: {Email}", email);
        
        return true;
    }
    
    public async Task<bool> ChangePassword(int userId, string newPassword, CancellationToken cancellationToken)
    {
        var credential = await _credentialRepository.GetCredentialById(userId);
        
        if (credential == null)
        {
            return false;
        }

        credential.PasswordHash = HashPassword(newPassword);
        credential.ResetPasswordToken = Guid.Empty;
        await _credentialRepository.UpdateCredential(credential, cancellationToken);

        return true;
    }
    
    public async Task<Guid> SetResetPasswordToken(int userId)
    {
        var credential = await _credentialRepository.GetCredentialById(userId);
        
        if (credential == null)
        {
            return Guid.Empty;
        }

        var resetToken = Guid.NewGuid();
        credential.ResetPasswordToken = resetToken;
        
        var result = await _credentialRepository.UpdateCredential(credential, new CancellationToken());
        
        if (!result)
        {
            return Guid.Empty;
        }

        return resetToken;
    }
    
    public async Task UpdateCredential(Credential credential, CancellationToken cancellationToken = default)
    {
        await _credentialRepository.UpdateCredential(credential, cancellationToken);
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
    
    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
    
    private string GenerateVerificationCode()
    {
        // Generate 6-digit verification code
        var random = new Random();
        var verificationCode = random.Next(100000, 999999).ToString();
        
        return verificationCode;
    }
}