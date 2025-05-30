using MassTransit;

namespace MeteorCloud.Messaging.ConsumerExtensions;

public static class ReceiveEndpointExtensions
{
    public static void ApplyStandardSettings(this IReceiveEndpointConfigurator endpoint)
    {
        endpoint.ConfigureConsumeTopology = false;
        endpoint.UseMessageRetry(r => r.Immediate(1)); // try once immediately
        endpoint.UseScheduledRedelivery(r =>
        {
            r.Intervals(
                TimeSpan.FromMinutes(2),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(60)
            );
        });
    }
}