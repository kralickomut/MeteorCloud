using AuthService.Services;
using MassTransit;
using MeteorCloud.Messaging.Events;

namespace AuthService.Consumers;

public class UserDeletedConsumer : IConsumer<UserDeletedEvent>
{
    private readonly ILogger<UserDeletedConsumer> _logger;
    private readonly CredentialManager _credentialManager;
    
    public UserDeletedConsumer(ILogger<UserDeletedConsumer> logger, CredentialManager credentialManager)
    {
        _logger = logger;
        _credentialManager = credentialManager;
    }


    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        var userDeletedEvent = context.Message;
        
        var success = await _credentialManager.DeleteCredentialsByUserIdAsync(userDeletedEvent.UserId);
        
        _logger.LogInformation("Deleted credential for user ID: {UserId}", userDeletedEvent.UserId);
        
        if (!success)
        {
            _logger.LogError("Failed to delete credential for user ID: {UserId}", userDeletedEvent.UserId);
        }
    }
}