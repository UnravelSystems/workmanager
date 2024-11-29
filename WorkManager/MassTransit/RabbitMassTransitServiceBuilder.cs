using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkManager.Configuration;
using WorkManager.Configuration.Interfaces;
using WorkManager.Configuration.ServiceBus;
using WorkManager.Models;
using WorkManager.Models.S3;
using WorkManager.WorkerManager;

namespace WorkManager.MassTransit;

[ServiceConfiguration(ServiceName = "mass_transit", ServiceType = "rabbit")]
public class RabbitMassTransitServiceBuilder : ExternalServiceBuilder
{
    public override void ConfigureServices(IServiceCollection serviceCollection, IConfigurationSection configuration)
    {
        serviceCollection.AddMassTransit(x =>
        {
            IServiceProvider sp = x.BuildServiceProvider();
            IOptions<RabbitMQWorkerOptions>? options = sp.GetService<IOptions<RabbitMQWorkerOptions>>();
            if (options is null)
            {
                throw new InvalidOperationException("RabbitMQ worker options are missing.");
            }

            RabbitMQWorkerOptions rabbitOptions = options.Value;
            x.AddSingleton<IWorkerManager<WorkRequest<MessageData, Metadata>>, MessageConsumer>();
            x.AddConsumer<MessageConsumer>().Endpoint(e => { e.Name = rabbitOptions.InQueue; });
            x.AddConsumer<FaultConsumer>().Endpoint(e => { e.Name = rabbitOptions.FaultQueue; });
            x.UsingRabbitMq((context, cfg) =>
            {
                string hostAddress = $"rabbitmq://{rabbitOptions.Host}:{rabbitOptions.Port}";
                cfg.Host(hostAddress, h =>
                {
                    h.Username(rabbitOptions.Username);
                    h.Password(rabbitOptions.Password);
                });

                //cfg.UseJsonSerializer();
                cfg.ConfigureJsonSerializerOptions(jsonOptions =>
                {
                    jsonOptions.IncludeFields = true;
                    jsonOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    return jsonOptions;
                });
                ((IReceiveConfigurator)cfg).ReceiveEndpoint(rabbitOptions.InQueue, configureEndpoint =>
                {
                    configureEndpoint.Consumer<MessageConsumer>(context);
                    if (configureEndpoint is IRabbitMqReceiveEndpointConfigurator r)
                    {
                        r.EnablePriority(2);
                        r.Durable = true;
                        r.ConcurrentMessageLimit = 1;
                    }
                });

                ((IReceiveConfigurator)cfg).ReceiveEndpoint(rabbitOptions.FaultQueue, configureEndpoint =>
                {
                    configureEndpoint.Consumer<FaultConsumer>(context);
                    if (configureEndpoint is IRabbitMqReceiveEndpointConfigurator r)
                    {
                        r.EnablePriority(2);
                        r.Durable = true;
                    }
                });
            });
        });

        serviceCollection.AddHostedService<Producer>();
    }
}