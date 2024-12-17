using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using WorkManager.Configuration;
using WorkManager.Configuration.Database.External;
using WorkManager.Configuration.Database.Mongo;
using WorkManager.Configuration.Interfaces;

namespace WorkManager.Database.Mongo;

/// <summary>
///     Wrapper for a MongoClient to automatically create based on a configuration file
/// </summary>
[ServiceConfiguration(ServiceName = "mongo")]
public class MongoClientServiceBuilder : IExternalServiceBuilder
{
    /// <summary>
    ///     Constructor which takes in an autoconfigured MongoOptions and the client settings
    ///     Sets default to ignore nulls
    /// </summary>
    /// <param name="serviceCollection">ServiceCollection to add the IMongoClient</param>
    /// <param name="configuration">any configuration section for this service</param>
    public void ConfigureServices(IServiceCollection serviceCollection, IConfigurationSection configuration)
    {
        serviceCollection.AddSingleton<IMongoClient>(sp =>
        {
            IOptions<MongoOptions>? options = sp.GetService<IOptions<MongoOptions>>();

            MongoClient mongoClient = new MongoClient(options.Value.ClientSettings);
            return mongoClient;
        });
        
        ConventionRegistry.Register("Ignore",
            new ConventionPack
            {
                new IgnoreIfNullConvention(true)
            },
            t => true);
    }
}