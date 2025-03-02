using AuthService.Persistence;
using AuthService.Services;
using MassTransit;
using MeteorCloud.Messaging.Events;

namespace AuthService.Consumers;

public class UserUpdatedConsumer : IConsumer<UserUpdatedEvent>
{
    private readonly ILogger<UserUpdatedConsumer> _logger;
    private readonly CredentialManager _credentialManager;

    public UserUpdatedConsumer(ILogger<UserUpdatedConsumer> logger, CredentialManager credentialManager)
    {
        _logger = logger;
        _credentialManager = credentialManager;
    }

    public async Task Consume(ConsumeContext<UserUpdatedEvent> context)
    {
        var userUpdatedEvent = context.Message;
        
        var credential = await _credentialManager.GetCredentialsByUserId(userUpdatedEvent.UserId);
        
        if (credential == null)
        {
            _logger.LogWarning("Credential not found for userId: {UserId}", userUpdatedEvent.UserId);
            return;
        }
        
        var success = await _credentialManager.UpdateCredentialsAsync(new Credential
        {
            Id = credential.Id,
            Email = userUpdatedEvent.Email,
            PasswordHash = credential.PasswordHash,
            UserId = userUpdatedEvent.UserId
        });
        
        _logger.LogInformation("Updated credential for email: {Email}", userUpdatedEvent.Email);
        
        if (!success)
        {
            _logger.LogError("Failed to update credential for email: {Email}", userUpdatedEvent.Email);
        }
    }
}