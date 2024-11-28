using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace WorkManager.Configuration.Database.External;

[ServiceConfiguration(ServiceName = "mongo")]
public class MongoClientWrapper : MongoClient
{
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