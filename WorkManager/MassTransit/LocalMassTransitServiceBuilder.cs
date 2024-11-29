using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkManager.Configuration;
using WorkManager.Configuration.Interfaces;
using WorkManager.WorkerManager;

namespace WorkManager.MassTransit;

[ServiceConfiguration(ServiceName = "mass_transit", ServiceType = "local")]
public class LocalMassTransitServiceBuilder : ExternalServiceBuilder
{
    public override void ConfigureServices(IServiceCollection serviceCollection, IConfigurationSection configuration)
    {
        serviceCollection.AddMassTransit(x =>
        {
            x.AddConsumer<MessageConsumer>();
            x.AddConsumer<FaultConsumer>();
            x.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); });
        });

        serviceCollection.AddHostedService<Producer>();
    }
}