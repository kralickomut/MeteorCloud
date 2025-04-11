using EmailService.Abstraction;
using MassTransit;
using MeteorCloud.Messaging.Events.Auth;

namespace EmailService.Consumers.Auth;

public class VerificationCodeResentConsumer : IConsumer<VerificationCodeResentEvent>
{
    private readonly ILogger<VerificationCodeResentConsumer> _logger;
    private readonly IEmailSender _emailSender;
    
    public VerificationCodeResentConsumer(ILogger<VerificationCodeResentConsumer> logger, IEmailSender emailSender)
    {
        _logger = logger;
        _emailSender = emailSender;
    }
    
    public async Task Consume(ConsumeContext<VerificationCodeResentEvent> context)
    {
        var message = context.Message;
        
        await _emailSender.SendEmailAsync(message.Email, "Verification Code Resent", 
            $"Your verification code is: {message.VerificationCode}");
        _logger.LogInformation("Event consumed: {EventName}", nameof(VerificationCodeResentEvent));
        _logger.LogInformation("Verification code resent email sent to {Email}", message.Email);
    }
}