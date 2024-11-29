using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkManager.Configuration;
using WorkManager.Configuration.Datastore.S3;
using WorkManager.Configuration.Interfaces;

namespace WorkManager.Datastore.S3;

[ServiceConfiguration(ServiceName = "S3")]
public class S3ClientServiceBuilder : ExternalServiceBuilder
{
    public override void ConfigureServices(IServiceCollection serviceCollection, IConfigurationSection configuration)
    {
        serviceCollection.AddSingleton<IAmazonS3>(sp =>
        {
            IOptions<S3Options>? options = sp.GetService<IOptions<S3Options>>();

            AmazonS3Config config = new AmazonS3Config
            {
                AuthenticationRegion = RegionEndpoint.USEast1.SystemName,
                ServiceURL = options.Value.ServiceURL,
                ForcePathStyle = true
            };
            AmazonS3Client client = new AmazonS3Client(options.Value.Credentials, config);
            return client;
        });
    }
}