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
    
    public async Task RegisterUserAsync(string email, string name, string password, CancellationToken cancellationToken)
    {
        var credential = new Credential
        {
            Email = email,
            PasswordHash = HashPasswordAsync(password),
            IsVerified = false,
            VerificationCode = GenerateVerificationCode(),
            VerificationExpiry = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        };
        
        var userId = await _credentialRepository.CreateCredential(credential, cancellationToken);
        
        if (!userId.HasValue)
        {
            throw new Exception("Failed to create credential. Email already in use.");
        }

        await _publishEndpoint.Publish(new UserRegisteredEvent(userId.Value, credential.Email, name, credential.VerificationCode));
        _logger.LogInformation("User registered event published for user email: {Email}", email);  
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

    private string HashPasswordAsync(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
    
    private string GenerateVerificationCode()
    {
        // Generate 6-digit verification code
        var random = new Random();
        var verificationCode = random.Next(100000, 999999).ToString();
        
        return verificationCode;
    }
}