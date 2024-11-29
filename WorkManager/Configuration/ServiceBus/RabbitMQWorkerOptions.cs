using System.ComponentModel.DataAnnotations;

namespace WorkManager.Configuration.ServiceBus;

[OptionsConfiguration(ServiceName = "mass_transit.rabbit")]
public class RabbitMQWorkerOptions
{
    [Required] public string Host { get; set; }

    [Required] [Range(1, 65535)] public int Port { get; set; } = 5672;

    [Required] public string Username { get; set; }

    [Required] public string Password { get; set; }

    public string InQueue { get; set; } = "work_item";
    public string OutQueue { get; set; } = "work_item_out";
    public string FaultQueue { get; set; } = "work_item_fault";
}