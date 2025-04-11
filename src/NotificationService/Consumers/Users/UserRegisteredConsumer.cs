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
        
        await _emailSender.SendEmailAsync(message.Email, "Welcome!", "Ahoj");
        await _emailSender.SendEmailAsync(message.Email, "Verify Your Account", $"This is a verification code: {message.VerificationCode}");
        
        _logger.LogInformation($"Email sent to {message.Email} with verification code {message.VerificationCode}");
    }
}