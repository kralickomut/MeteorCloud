using EmailService.Abstraction;
using MassTransit;
using MeteorCloud.Messaging.Events.Auth;

namespace EmailService.Consumers.Auth;

public class PasswordResetRequiredConsumer : IConsumer<PasswordResetRequiredEvent>
{
    private readonly ILogger<PasswordResetRequiredConsumer> _logger;
    private readonly IEmailSender _emailSender;
    
    public PasswordResetRequiredConsumer(ILogger<PasswordResetRequiredConsumer> logger, IEmailSender emailSender)
    {
        _logger = logger;
        _emailSender = emailSender;
    }
    
    
    public async Task Consume(ConsumeContext<PasswordResetRequiredEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Event consumed: {EventName}", nameof(PasswordResetRequiredEvent));
        
        string link = $"http://localhost:4200/reset-password/{message.Token}";

        string html = $@"
            <p>Hello,</p>
            <p>You requested a password reset. Click the button below to reset your password:</p>
            <p>
                <a href='{link}' style='
                    display: inline-block;
                    padding: 10px 20px;
                    background-color: #6366f1;
                    color: white;
                    text-decoration: none;
                    border-radius: 6px;
                    font-weight: bold;
                '>Reset Password</a>
            </p>
            <p>If you didn’t request this, you can safely ignore this email.</p>
            <p>— MeteorCloud Team</p>";

        await _emailSender.SendEmailAsync(message.Email, "Password Reset Required", html);
    }
}