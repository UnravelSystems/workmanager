using System.ComponentModel.DataAnnotations;

namespace WorkManager.Configuration.Job;

[OptionsConfiguration(ServiceName = "job_manager.mongo")]
public class MongoJobManagerOptions
{
    [Required]
    public string Collection { get; set; }
    
    [Required]
    public string Database { get; set; }
}