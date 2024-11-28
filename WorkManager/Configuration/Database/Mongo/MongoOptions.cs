using System.ComponentModel.DataAnnotations;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace WorkManager.Configuration.Database.External;

[OptionsConfiguration(ServiceName = "mongo")]
public class MongoOptions
{
    [Required]
    public string URI { get; set; }
    [Required]
    [Range(1, 65535)]
    public int Port { get; set; } = 27017;
    [Required]
    public string Password { get; set; }
    [Required]
    public string Username { get; set; }

    public MongoCredential Credentials => MongoCredential.CreateCredential(null, Username, Password);

    public MongoClientSettings ClientSettings
    {
        get => new MongoClientSettings()
        {
            Scheme = ConnectionStringScheme.MongoDB,
            Server = new MongoServerAddress(URI, Port),
            Credential = Credentials
        };
    }
}