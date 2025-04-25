using EmailService.Consumers.Auth;
using EmailService.Consumers.Users;
using EmailService.Persistence;
using EmailService.Senders;
using MassTransit;
using EmailService.Abstraction;
using EmailService.Consumers.Workspace;
using EmailService.Features;
using EmailService.Hubs;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.Workspace;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;

namespace EmailService.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddTransient<IEmailSender, SmtpEmailSender>();

        services.AddSingleton<GetUnreadNotificationsValidator>();
        services.AddScoped<GetUnreadNotificationsHandler>();
        
        services.AddSingleton<MarkAsReadValidator>();
        services.AddScoped<MarkAsReadHandler>();
        
        services.AddTransient<DatabaseInitializer>();
        services.AddSingleton<DapperContext>();
        services.AddScoped<NotificationRepository>();

        services.AddHttpClient<MSHttpClient>();

        
        // Configuration for the SignalR hub
        services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
        services.AddSignalR();

        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Notification Service API", Version = "v1" });
        });

        
        services.AddMassTransit(config =>
        {
            config.AddConsumer<UserRegisteredConsumer>();
            config.AddConsumer<VerificationCodeResentConsumer>();
            config.AddConsumer<WorkspaceInviteConsumer>();
            config.AddConsumer<WorkspaceInvitationMatchOnRegisterConsumer>();
            config.AddConsumer<WorkspaceDeletedConsumer>();
            config.AddConsumer<WorkspaceInvitationResponseConsumer>();

            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ReceiveEndpoint("email-service-user-registered-queue", e =>
                {
                    e.Bind("user-registered", x => x.ExchangeType = "fanout");
                    e.ConfigureConsumer<UserRegisteredConsumer>(context);
                });
                
                cfg.ReceiveEndpoint("email-service-verification-code-resent-queue", e =>
                {
                    e.Bind("verification-code-resent", x => x.ExchangeType = "fanout");
                    e.ConfigureConsumer<VerificationCodeResentConsumer>(context);
                });
                
                cfg.ReceiveEndpoint("email-service-workspace-invite-queue", e =>
                {
                    e.Bind("workspace-invite", x => x.ExchangeType = "fanout");
                    e.ConfigureConsumer<WorkspaceInviteConsumer>(context);
                });
                
                cfg.ReceiveEndpoint("email-service-workspace-invitation-match-on-register-queue", e =>
                {
                    e.Bind("workspace-invitation-match-on-register", x => x.ExchangeType = "fanout");
                    e.ConfigureConsumer<WorkspaceInvitationMatchOnRegisterConsumer>(context);
                });
                
                cfg.ReceiveEndpoint("email-service-workspace-deleted-queue", e =>
                {
                    e.Bind("workspace-deleted", x => x.ExchangeType = "fanout");
                    e.ConfigureConsumer<WorkspaceDeletedConsumer>(context);
                });
                
                cfg.ReceiveEndpoint("email-service-workspace-invitation-response-queue", e =>
                {
                    e.Bind("workspace-invitation-response", x => x.ExchangeType = "fanout");
                    e.ConfigureConsumer<WorkspaceInvitationResponseConsumer>(context);
                });
            });
        });


        
        return services;
    }
}