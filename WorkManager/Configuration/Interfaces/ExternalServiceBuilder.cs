using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WorkManager.Configuration.Interfaces;

public abstract class ExternalServiceBuilder
{
    public abstract void ConfigureServices(IServiceCollection serviceCollection, IConfigurationSection configuration);
}