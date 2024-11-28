using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkManager.Configuration;
using WorkManager.MassTransit;
using WorkManager.Models;
using WorkManager.Models.S3;
using WorkManager.Models.Tree;
using WorkManager.WorkerManager;

namespace WorkManager.Worker;

/// <summary>
/// Simple worker for handling tree nodes, this is used for testing the system
/// </summary>
[Worker(WorkerName = "NodeWorker")]
public class NodeWorker : IFileWorker<WorkRequest<Metadata, MessageData>>
{
    private readonly ILogger<NodeWorker> _logger;
    private IWorkerManager<WorkRequest<Metadata, MessageData>> _workerManager;
    public NodeWorker(ILogger<NodeWorker> logger)
    {
        _logger = logger;
    }

    public void SetWorkerManager(IWorkerManager manager)
    {
        _workerManager = (IWorkerManager<WorkRequest<Metadata, MessageData>>)manager;
    }

    public bool Accepts(WorkRequest<Metadata, MessageData> request)
    {
        return true;
    }

    public void Process(WorkRequest<Metadata, MessageData> request)
    {
        var jsonConvert = request.Data.Root;
        foreach (StringTreeNode child in jsonConvert.Children)
        {
            _workerManager.AddWorkItem(new WorkRequest<Metadata, MessageData>
            {
                JobId = request.JobId,
                Metadata = request.Metadata,
                Data = new MessageData()
                {
                    Root = child
                },
                IsCreated = true
            });
        }
    }

    public void ProcessFile(WorkRequest<Metadata, MessageData> file)
    {
        throw new NotImplementedException();
    }
}