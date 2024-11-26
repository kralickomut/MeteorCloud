using FileService.Application.Consumers;
using MassTransit;
using Messaging.Base.Extensions;

namespace FileService.Presentation.Configuration;

public static class ConfigurationSettings
{
    public static void AddProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransitWithRabbitMQ(
            configuration["RabbitMq:Host"],
            configureConsumers: cfg =>
            {
                cfg.AddConsumer<FileUploadedConsumer>();
            },
            configureRabbitMq: (rabbitCfg, context) =>
            {
                rabbitCfg.ReceiveEndpoint("file-service-queue", ep =>
                {
                    ep.ConfigureConsumer<FileUploadedConsumer>(context);
                });
            });
        

        services.AddScoped<FileUploadedConsumer>();
    }
}