using System.Reflection;
using MassTransit.Transports.Fabric;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using MassTransit.MultiBus;

namespace MeteorCloud.Messaging.Extensions;

public static class MassTransitExtensions
{
    public static void AddMassTransitWithRabbitMQ(this IServiceCollection services, string rabbitMqHost, Assembly assembly)
    {
        services.AddMassTransit(cfg =>
        {
            cfg.SetKebabCaseEndpointNameFormatter(); // Standardized queue naming

            // ✅ Auto-register all consumers found in this microservice
            cfg.AddConsumers(assembly);

            cfg.UsingRabbitMq((context, rabbitCfg) =>
            {
                rabbitCfg.Host(rabbitMqHost, h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                // ✅ Auto-register all consumers
                rabbitCfg.ConfigureEndpoints(context);
            });
        });
    }
}