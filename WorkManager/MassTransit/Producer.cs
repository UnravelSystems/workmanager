using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Hosting;
using WorkManager.Models;
using WorkManager.Models.S3;
using WorkManager.Models.Tree;
using WorkManager.WorkerManager;

namespace WorkManager.MassTransit;

public class Producer : BackgroundService
{
    private readonly IBus _bus;

    public Producer(IBus bus)
    {
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string dict = await File.ReadAllTextAsync(@"data.json", stoppingToken);
        StringTreeNode? data = JsonSerializer.Deserialize<StringTreeNode>(dict);

        for (int i = 0; i < 1; i++)
        {
            WorkRequest<MessageData, Metadata> message = new WorkRequest<MessageData, Metadata>
            {
                JobId = Guid.NewGuid().ToString(),
                Data = new MessageData
                {
                    Root = data
                },
                Metadata = new Metadata
                {
                    Bucket = "Bucket",
                    Key = "Key",
                    ResultBucket = "ResultBucket"
                }
            };
            await _bus.Publish(message, ctx => { ctx.SetPriority(1); }, stoppingToken);

            //await Task.Delay(100000000, stoppingToken);
        }
    }
}