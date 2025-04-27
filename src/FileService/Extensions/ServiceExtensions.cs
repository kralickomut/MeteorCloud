using Azure.Storage.Blobs;
using FileService.Consumers;
using FileService.Features;
using FileService.Services;
using MassTransit;
using MeteorCloud.Communication;
using MeteorCloud.Messaging.Events.File;
using Microsoft.OpenApi.Models;

namespace FileService.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register IConfiguration explicitly
        services.AddSingleton(configuration);

        services.AddSingleton<UploadFileRequestValidator>();
        services.AddScoped<UploadFileHandler>();

        services.AddSingleton<DeleteFileRequestValidator>();
        services.AddScoped<DeleteFileHandler>();
        
        services.AddSingleton<DeleteFolderRequestValidator>();
        services.AddScoped<DeleteFolderHandler>();
        
        services.AddScoped<DownloadFileHandler>();
        
        services.AddSwaggerGen(options =>
        {
            options.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
        });
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "File Service API", Version = "v1" });
        });
        
        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.AddConsumer<WorkspaceDeletedConsumer>();
            
            busConfigurator.UsingRabbitMq((context, rabbitCfg) =>
            {
                rabbitCfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                
                rabbitCfg.Message<FileUploadedEvent>(x => x.SetEntityName("file-uploaded"));
                rabbitCfg.Message<FileDeletedEvent>(x => x.SetEntityName("file-deleted"));
                rabbitCfg.Message<FolderDeletedEvent>(x => x.SetEntityName("folder-deleted"));
                
                rabbitCfg.ReceiveEndpoint("file-service-workspace-deleted-queue", e =>
                {
                    e.Bind("workspace-deleted", x =>
                    {
                        x.ExchangeType = "fanout";
                    });
                    
                    e.ConfigureConsumer<WorkspaceDeletedConsumer>(context);
                });
                
                rabbitCfg.ConfigureEndpoints(context);
            });
        });
        
        services.AddHttpContextAccessor();
        services.AddTransient<AuthHeaderForwardingHandler>();
        services.AddHttpClient<MSHttpClient>()
            .AddHttpMessageHandler<AuthHeaderForwardingHandler>();
        
        // Register Azure Blob Storage client
        services.AddSingleton(s =>
        {
            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            return new BlobServiceClient(connectionString);
        });

        services.AddScoped<BlobStorageService>();
        
        return services;
    }
}