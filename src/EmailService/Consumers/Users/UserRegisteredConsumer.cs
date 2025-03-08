using EmailService.Abstraction;
using MassTransit;
using MeteorCloud.Messaging.Events;

namespace EmailService.Consumers.Users;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(IEmailSender emailSender, ILogger<UserRegisteredConsumer> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation($"Received UserRegisteredEvent for {message.Email}");

        var emailBody = $"Hello {message.FirstName},\n\nWelcome to our platform!";
        
        await _emailSender.SendEmailAsync(message.Email, "Welcome!", emailBody);
    }
}