using EmailService.Abstraction;
using EmailService.Hubs;
using EmailService.Persistence;
using MassTransit;
using MeteorCloud.Messaging.Events;
using Microsoft.AspNetCore.SignalR;

namespace EmailService.Consumers.Users;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly IEmailSender _emailSender;
    private readonly NotificationRepository _notificationRepository;
    private readonly ILogger<UserRegisteredConsumer> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

    public UserRegisteredConsumer(IEmailSender emailSender, NotificationRepository notificationRepository, ILogger<UserRegisteredConsumer> logger, IHubContext<NotificationHub> notificationHub)
    
    {
        _emailSender = emailSender;
        _logger = logger;
        _notificationRepository = notificationRepository;
        _hubContext = notificationHub;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var message = context.Message;
        
        await _emailSender.SendEmailAsync(message.Email, "Welcome!", "Welcome to MeteorCloud!");
        await _emailSender.SendEmailAsync(message.Email, "Verify Your Account", $"This is a verification code: {message.VerificationCode}");

        var userId = message.UserId;
        
        await _notificationRepository.CreateAsync(new Notification
        {
            UserId = userId,
            Title = "Welcome!",
            Message = "Welcome to MeteorCloud, your digital workspace!",
            IsRead = false
        });

        await _notificationRepository.CreateAsync(new Notification
        {
            UserId = userId,
            Title = "Collaborate",
            Message = "Invite others and work together in shared workspaces.",
            IsRead = false
        });

        await _notificationRepository.CreateAsync(new Notification
        {
            UserId = userId,
            Title = "Store files",
            Message = "Securely upload and preview your files in the cloud.",
            IsRead = false
        });
        
        _logger.LogInformation($"Email sent to {message.Email} with verification code {message.VerificationCode}");
    }
}