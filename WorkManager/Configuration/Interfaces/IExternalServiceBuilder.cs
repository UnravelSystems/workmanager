using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WorkManager.Configuration.Interfaces;

public interface IExternalServiceBuilder
{
    public void ConfigureServices(IServiceCollection serviceCollection, IConfigurationSection configuration);
}