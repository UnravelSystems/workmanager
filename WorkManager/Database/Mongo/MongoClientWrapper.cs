using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using WorkManager.Configuration;
using WorkManager.Configuration.Database.External;

namespace WorkManager.Database.Mongo;

/// <summary>
///     Wrapper for a MongoClient to automatically create based on a configuration file
/// </summary>
[ServiceConfiguration(ServiceName = "mongo")]
public class MongoClientWrapper : MongoClient
{
    /// <summary>
    ///     Constructor which takes in an autoconfigured MongoOptions and the client settings
    ///     Sets default to ignore nulls
    /// </summary>
    /// <param name="options">Autoconfigured options from a configuration file</param>
    public MongoClientWrapper(IOptions<MongoOptions> options) : base(options.Value.ClientSettings)
    {
        ConventionRegistry.Register("Ignore",
            new ConventionPack
            {
                new IgnoreIfNullConvention(true)
            },
            t => true);
    }
}