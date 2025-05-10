using MassTransit;
using MeteorCloud.Messaging.Events;

namespace UserService.Consumers;

public class ProfileImageUploadedConsumer : IConsumer<ProfileImageUploadedEvent>
{
    
    private readonly ILogger<ProfileImageUploadedConsumer> _logger;
    private readonly Services.UserService _userService;

    public ProfileImageUploadedConsumer(ILogger<ProfileImageUploadedConsumer> logger, Services.UserService userService)
    {
        _userService = userService;
        _logger = logger;

    }


    public async Task Consume(ConsumeContext<ProfileImageUploadedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received ProfileImageUploadedEvent: UserId={UserId}, ImageUrl={ImageUrl}", message.UserId, message.ImageUrl);
        
        // Update the user's profile image URL in the database
        var user = await _userService.GetUserByIdAsync(message.UserId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found. Cannot update profile image.", message.UserId);
            return;
        }
        
        user.ProfilePictureUrl = message.ImageUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await _userService.UpdateUserAsync(user);
    }
}