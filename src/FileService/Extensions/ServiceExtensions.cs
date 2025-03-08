using Azure.Storage.Blobs;
using FileService.Features;
using FileService.Services;
using MassTransit;
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
        
        services.AddSwaggerGen(options =>
        {
            options.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
        });
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "File Service API", Version = "v1" });
        });
        /*
        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.UsingRabbitMq((context, rabbitCfg) =>
            {
                rabbitCfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                
                rabbitCfg.ConfigureEndpoints(context);
                
            });
        });
        */
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