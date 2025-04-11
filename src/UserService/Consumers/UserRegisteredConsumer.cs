using MassTransit;
using MeteorCloud.Messaging.Events;
using Microsoft.AspNetCore.Identity;
using UserService.Persistence;
using UserService.Services;

namespace UserService.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    
    private readonly ILogger<UserRegisteredConsumer> _logger;
    private readonly Services.UserService _userService;
    
    public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger, Services.UserService userService)
    {
        _logger = logger;
        _userService = userService;
    }
    
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var userRegisteredEvent = context.Message;

        var user = new User
        {
            Id = userRegisteredEvent.UserId,
            Name = userRegisteredEvent.Name,
            Email = userRegisteredEvent.Email,
            RegistrationDate = DateTime.UtcNow,
            InTotalWorkspaces = 0,
            UpdatedAt = null
        };

        await _userService.CreateUserAsync(user);
        
        _logger.LogInformation("User registration consumed: {UserId}", userRegisteredEvent.UserId);
    }
}