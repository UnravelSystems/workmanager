using System.ComponentModel.DataAnnotations;
using Amazon.Runtime;

namespace WorkManager.Configuration.Datastore.S3;

[OptionsConfiguration(ServiceName = "S3")]
public class S3Options
{
    [Required] public string? Host { get; set; }

    [Required] [Range(1, 65534)] public int Port { get; set; } = 9000;

    [Required] public string? Username { get; set; }

    [Required] public string? Password { get; set; }

    [Required] public bool UseSSL { get; set; } = false;

    public AWSCredentials Credentials => new BasicAWSCredentials(Username, Password);

    public string ServiceURL => UseSSL ? $"https://{Host}:{Port}" : $"http://{Host}:{Port}";
}