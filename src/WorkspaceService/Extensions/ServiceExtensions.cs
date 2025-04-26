using MassTransit;
using MeteorCloud.Caching.Abstraction;
using MeteorCloud.Caching.Services;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.Workspace;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using WorkspaceService.Consumers;
using WorkspaceService.Features;
using WorkspaceService.Hubs;
using WorkspaceService.Persistence;
using WorkspaceService.Services;

namespace WorkspaceService.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register IConfiguration explicitly
        services.AddSingleton(configuration);

        // Register DatabaseInitializer and pass IConfiguration
        services.AddSingleton<DatabaseInitializer>();

        // Ensure DapperContext receives IConfiguration
        services.AddSingleton<DapperContext>();
        
        // Register WorkspaceRepository
        services.AddScoped<WorkspaceRepository>();
        
        // Register WorkspaceManager
        services.AddScoped<WorkspaceManager>();

        services.AddSingleton<CreateWorkspaceValidator>();
        services.AddScoped<CreateWorkspaceHandler>();
        
        services.AddSingleton<InviteToWorkspaceValidator>();
        services.AddScoped<InviteToWorkspaceHandler>();
        
        services.AddSingleton<GetUserWorkspacesValidator>();
        services.AddScoped<GetUserWorkspacesHandler>();
        
        services.AddSingleton<DeleteWorkspaceValidator>();
        services.AddScoped<DeleteWorkspaceHandler>();

        services.AddSingleton<GetWorkspaceByIdValidator>();
        services.AddScoped<GetWorkspaceByIdHandler>();

        services.AddSingleton<RespondToInviteValidator>();
        services.AddScoped<RespondToInviteHandler>();

        services.AddSingleton<GetWorkspaceInvitationByTokenValidator>();
        services.AddScoped<GetWorkspaceInvitationByTokenHandler>();
        
        services.AddSingleton<UpdateWorkspaceValidator>();
        services.AddScoped<UpdateWorkspaceHandler>();
        
        services.AddSingleton<ChangeUserRoleValidator>();
        services.AddScoped<ChangeUserRoleHandler>();
        
        services.AddSingleton<RemoveUserValidator>();
        services.AddScoped<RemoveUserHandler>();
        
        services.AddSingleton<GetWorkspaceInvitationsHistoryValidator>();
        services.AddScoped<GetWorkspaceInvitationsHistoryHandler>();
        
        services.AddSingleton<IsUserInWorkspaceRequestValidator>();
        services.AddScoped<IsUserInWorkspaceHandler>();

        services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
        services.AddSignalR();
        
        
        // Get Redis connection details from environment variables
        var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
        var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
        var redisConnectionString = $"{redisHost}:{redisPort}";

        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString));

        // Register Redis Cache Service from shared library
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddHttpContextAccessor();
        services.AddTransient<AuthHeaderForwardingHandler>();
        services.AddHttpClient<MSHttpClient>()
            .AddHttpMessageHandler<AuthHeaderForwardingHandler>();
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Workspace Service API", Version = "v1" });
        });
        
        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.AddConsumer<UserRegisteredConsumer>();
            
            busConfigurator.UsingRabbitMq((context, rabbitCfg) =>
            {
                rabbitCfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                
                rabbitCfg.Message<WorkspaceCreatedEvent>(x => x.SetEntityName("workspace-created"));
                rabbitCfg.Message<WorkspaceInviteEvent>(x => x.SetEntityName("workspace-invite"));
                rabbitCfg.Message<WorkspaceDeletedEvent>(x => x.SetEntityName("workspace-deleted"));    
                rabbitCfg.Message<WorkspaceInvitationMatchOnRegisterEvent>(x => x.SetEntityName("workspace-invitation-match-on-register"));
                rabbitCfg.Message<WorkspaceInvitationResponseEvent>(x => x.SetEntityName("workspace-invitation-response"));
                rabbitCfg.Message<WorkspaceOwnerChangedEvent>(x => x.SetEntityName("workspace-owner-changed"));
                rabbitCfg.Message<WorkspaceUserRemovedEvent>(x => x.SetEntityName("workspace-user-removed"));
                
                rabbitCfg.ReceiveEndpoint("workspace-service-user-registered-queue", e =>
                {
                    e.Bind("user-registered", x =>
                    {
                        x.ExchangeType = "fanout";
                    });
                    
                    e.ConfigureConsumer<UserRegisteredConsumer>(context);
                });
                
                
                rabbitCfg.ConfigureEndpoints(context);
                
            });
        });
        
        return services;
    }
}