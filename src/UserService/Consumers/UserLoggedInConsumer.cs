using MassTransit;
using MeteorCloud.Messaging.Events;

namespace UserService.Consumers;

public class UserLoggedInConsumer : IConsumer<UserLoggedInEvent>
{
    private readonly Services.UserService _userService;
    private readonly ILogger<UserLoggedInConsumer> _logger;
    
    public UserLoggedInConsumer(Services.UserService userService, ILogger<UserLoggedInConsumer> logger)
    {
        _userService = userService;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<UserLoggedInEvent> context)
    {
        var message = context.Message;

        var user = await _userService.GetUserByIdAsync(message.UserId);

        if (user is null)
        {
            throw new Exception("User logged in but not found in UserService.");
        }
        
        user.LastLogin = DateTime.UtcNow;
        await _userService.UpdateUserAsync(user);
    }
}