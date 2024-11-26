using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Base.Extensions;

public static class MassTransitExtensions
{
    public static IServiceCollection AddMassTransitWithRabbitMQ(
        this IServiceCollection services,
        string rabbitMqHost,
        string username = "guest",
        string password = "guest",
        Action<IBusRegistrationConfigurator> configureConsumers = null,
        Action<IRabbitMqBusFactoryConfigurator, IRegistrationContext> configureRabbitMq = null)
    {
        services.AddMassTransit(config =>
        {
            // Register consumers if provided
            configureConsumers?.Invoke(config);

            // Configure RabbitMQ transport
            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                // Additional RabbitMQ configuration
                configureRabbitMq?.Invoke(cfg, context);
            });
        });

        services.AddMassTransitHostedService(); // Ensure the bus starts with the app
        return services;
    }
}