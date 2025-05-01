using AuditService.Consumers.File;
using AuditService.Consumers.Workspace;
using AuditService.Features.File;
using AuditService.Features.Workspace;
using AuditService.Persistence;
using MassTransit;
using MeteorCloud.Communication;
using Microsoft.OpenApi.Models;

namespace AuditService.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DatabaseInitializer>();

        // Ensure DapperContext receives IConfiguration
        services.AddSingleton<DapperContext>();
        
        // Register repositories
        services.AddScoped<AuditRepository>();

        services.AddSingleton<GetFileHistoryByWorkspaceIdValidator>();
        services.AddScoped<GetFileHistoryByWorkspaceIdHandler>();
        
        services.AddSingleton<GetRecentWorkspaceIdsValidator>();
        services.AddScoped<GetRecentWorkspaceIdsHandler>();
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Workspace Service API", Version = "v1" });
        });
        
        services.AddHttpContextAccessor();
        services.AddTransient<AuthHeaderForwardingHandler>();
        services.AddHttpClient<MSHttpClient>()
            .AddHttpMessageHandler<AuthHeaderForwardingHandler>();
        
        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.AddConsumer<FileUploadedConsumer>();
            busConfigurator.AddConsumer<WorkspaceDeletedConsumer>();
            busConfigurator.AddConsumer<FileDeletedConsumer>();
            
            busConfigurator.UsingRabbitMq((context, rabbitCfg) =>
            {
                rabbitCfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                
                rabbitCfg.ReceiveEndpoint("audit-service-file-uploaded-queue", e =>
                {
                    e.Bind("file-uploaded", x =>
                    {
                        x.ExchangeType = "fanout";
                    });
                    
                    e.ConfigureConsumer<FileUploadedConsumer>(context);
                });
                
                rabbitCfg.ReceiveEndpoint("audit-service-workspace-deleted-queue", e =>
                {
                    e.Bind("workspace-deleted", x =>
                    {
                        x.ExchangeType = "fanout";
                    });
                    
                    e.ConfigureConsumer<WorkspaceDeletedConsumer>(context);
                });
                
                rabbitCfg.ReceiveEndpoint("audit-service-file-deleted-queue", e =>
                {
                    e.Bind("file-deleted", x =>
                    {
                        x.ExchangeType = "fanout";
                    });
                    
                    e.ConfigureConsumer<FileDeletedConsumer>(context);
                });
                
                
                rabbitCfg.ConfigureEndpoints(context);
                
            });
        });
        
        return services;
    }
}