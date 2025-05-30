using System.Text.Json;
using EmailService.Abstraction;
using EmailService.Hubs;
using EmailService.Persistence;
using MassTransit;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.Workspace;
using MeteorCloud.Shared.ApiResults.SharedDto;
using Microsoft.AspNetCore.SignalR;

namespace EmailService.Consumers.Workspace;

public class WorkspaceInviteConsumer : IConsumer<WorkspaceInviteEvent>
{
    private readonly MSHttpClient _httpClient;
    private readonly ILogger<WorkspaceInviteConsumer> _logger;
    private readonly IEmailSender _emailService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly NotificationRepository _notificationRepository;
    
    public WorkspaceInviteConsumer(
        ILogger<WorkspaceInviteConsumer> logger, 
        IEmailSender emailService, 
        MSHttpClient httpClient, 
        IHubContext<NotificationHub> hubContext,
        NotificationRepository notificationRepository)
    {
        _logger = logger;
        _emailService = emailService;
        _httpClient = httpClient;
        _hubContext = hubContext;
        _notificationRepository = notificationRepository;
    }
    
    
    public async Task Consume(ConsumeContext<WorkspaceInviteEvent> context)
    {
        var message = context.Message;
        
        var url = MicroserviceEndpoints.UserService.GetUserByEmail(message.Email);
        var response = await _httpClient.GetAsync<object>(url);
        
        if (!response.Success)
        {
            var subject = "You have been invited to join a workspace!";
            string link = $"http://localhost:4200/register";
            var body = $@"
            <h1>Welcome to MeteorCloud!</h1>
            <p>You have been invited to join a workspace.</p>
            <p>To accept the invitation and get more information, please create an account and confirm it in notifications.</p>
            <p>
                <a href='{link}' style='
                    display: inline-block;
                    padding: 10px 20px;
                    background-color: #6366f1;
                    color: white;
                    text-decoration: none;
                    border-radius: 6px;
                    font-weight: bold;
                '>Register</a>
            </p>
            ";
        
            await _emailService.SendEmailAsync(message.Email, subject, body);
            _logger.LogInformation("Email sent to {Email} for workspace invitation.", message.Email);
            
        } else {
            int userId = ((JsonElement)response.Data!).GetInt32();
            
            // Get workspace details
            var wsUrl = MicroserviceEndpoints.WorkspaceService.GetWorkspaceById(message.WorkspaceId);
            var workspace = await _httpClient.GetAsync<WorkspaceModel>(wsUrl);
            
            if (!workspace.Success) 
            {
                _logger.LogError("Failed to get workspace details for ID {WorkspaceId}.", message.WorkspaceId);
                return;
            }
            
            var notification = new Notification
            {
                UserId = userId,
                Title = "Workspace Invitation",
                Message = $"{message.Token}-You have been invited to join a workspace: {workspace.Data.Name}.", 
                IsRead = false,
                WorkspaceId = message.WorkspaceId
            };

            var newNotification = await _notificationRepository.CreateAsync(notification);
            
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", newNotification);
            _logger.LogInformation("Notification sent to user {UserId} for workspace invitation.", userId);
        }
        
        
    }
}