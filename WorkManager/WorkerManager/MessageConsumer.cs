using MassTransit;
using Microsoft.Extensions.Logging;
using WorkManager.Database;
using WorkManager.Datastore;
using WorkManager.Job;
using WorkManager.Models;
using WorkManager.Models.S3;
using WorkManager.Models.Tree;
using WorkManager.Worker;

namespace WorkManager.WorkerManager;

public class MessageData
{
    public StringTreeNode? Root { get; set; }
}

public class RabbitWorkRequest : WorkRequest<MessageData, Metadata>
{
}

/// <summary>
///     Consumer and worker manager for a basic work request containing metadata and a tree
/// </summary>
public class MessageConsumer : IConsumer<WorkRequest<MessageData, Metadata>>,
    IWorkerManager<WorkRequest<MessageData, Metadata>>
{
    private readonly IBus _bus;
    private readonly IDatastore _datastore;
    private readonly IDocumentStore<TreeDocument> _documentStore;
    private readonly IJobManager _jobManager;
    private readonly ILogger<MessageConsumer> _logger;
    private readonly IEnumerable<IWorker<WorkRequest<MessageData, Metadata>>> _workers;

    public MessageConsumer(ILogger<MessageConsumer> logger, IBus bus, IJobManager jobManager, IDatastore datastore,
        IDocumentStore<TreeDocument> documentStore,
        IEnumerable<IWorker<WorkRequest<MessageData, Metadata>>> workers)
    {
        _logger = logger;
        _bus = bus;
        _jobManager = jobManager;
        _datastore = datastore;
        _documentStore = documentStore;
        _workers = workers;

        // TODO decouple things so that there is no circular dependency
        foreach (IWorker<WorkRequest<MessageData, Metadata>> worker in _workers)
        {
            worker.SetWorkerManager(this);
        }
    }

    public Task Consume(ConsumeContext<WorkRequest<MessageData, Metadata>> context)
    {
        WorkRequest<MessageData, Metadata> workRequest = context.Message;
        string? jobId = workRequest.JobId;
        if (!workRequest.IsCreated)
        {
            _jobManager.AddTask(jobId, null);
        }

        HandleWorkItem(workRequest);

        long activeJobs = _jobManager.RemoveTask(jobId, null);
        if (_jobManager.IsJobFinished(jobId))
        {
            _logger.LogInformation(
                $"Job {jobId} has finished | removed={_jobManager.FinishJob(workRequest.JobId)} {Thread.CurrentThread.ManagedThreadId}");
        }

        return Task.CompletedTask;
    }

    public void HandleWorkItem(WorkRequest<MessageData, Metadata> workRequest)
    {
        _logger.LogInformation("Message received: {@Message}", workRequest);
        foreach (IWorker<WorkRequest<MessageData, Metadata>> worker in _workers)
        {
            if (worker.Accepts(workRequest))
            {
                worker.Process(workRequest);
            }
        }
    }

    public void AddWorkItem(WorkRequest<MessageData, Metadata> workItem)
    {
        using (MemoryStream memStream = new MemoryStream())
        using (StreamWriter writer = new StreamWriter(memStream))
        {
            writer.Write(workItem.Data);
            writer.Flush();
            _documentStore.AddDocument(
                new TreeDocument(
                    workItem.JobId,
                    Guid.NewGuid().ToString(),
                    workItem.Data.Root,
                    workItem.Metadata)
            );
            _datastore.StoreFile("test", $"{workItem.JobId}/{workItem.Data.Root.Value}", memStream);
        }

        StringTreeNode? node = workItem.Data.Root;
        if (node != null && node.Children != null && node.Children.Any())
        {
            _jobManager.AddTask(workItem.JobId, null);
            _bus.Publish(workItem, ctx => { ctx.SetPriority(2); });
        }
    }
}