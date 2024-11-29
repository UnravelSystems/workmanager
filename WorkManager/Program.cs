using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using WorkManager.Configuration;

namespace WorkManager;

internal class Program
{
    public static void Main(string[] args)
    {
        Configure(args).Build().Run();
    }

    public static IHostBuilder Configure(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, builder) => builder.AddJsonFile("configuration.json"))
            .ConfigureServices((ctx, services) =>
            {
                services.RegisterOptionsFromConfiguration(ctx.Configuration);
                services.RegisterServicesFromConfiguration(ctx.Configuration);
                services.RegisterWorkers(ctx.Configuration);
            });
    }
}