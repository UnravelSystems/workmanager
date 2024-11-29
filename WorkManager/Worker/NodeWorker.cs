using Microsoft.Extensions.Logging;
using WorkManager.Configuration;
using WorkManager.Models;
using WorkManager.Models.S3;
using WorkManager.Models.Tree;
using WorkManager.WorkerManager;

namespace WorkManager.Worker;

/// <summary>
///     Simple worker for handling tree nodes, this is used for testing the system
/// </summary>
[Worker(WorkerName = "NodeWorker")]
public class NodeWorker : IFileWorker<WorkRequest<MessageData, Metadata>>
{
    private readonly ILogger<NodeWorker> _logger;
    private IWorkerManager<WorkRequest<MessageData, Metadata>> _workerManager;

    public NodeWorker(ILogger<NodeWorker> logger)
    {
        _logger = logger;
    }

    public void SetWorkerManager(IWorkerManager manager)
    {
        _workerManager = (IWorkerManager<WorkRequest<MessageData, Metadata>>)manager;
    }

    public bool Accepts(WorkRequest<MessageData, Metadata> request)
    {
        return true;
    }

    public void Process(WorkRequest<MessageData, Metadata> request)
    {
        StringTreeNode? jsonConvert = request.Data.Root;
        foreach (StringTreeNode child in jsonConvert.Children)
        {
            _workerManager.AddWorkItem(new WorkRequest<MessageData, Metadata>
            {
                JobId = request.JobId,
                Metadata = request.Metadata,
                Data = new MessageData
                {
                    Root = child
                },
                IsCreated = true
            });
        }
    }

    public void ProcessFile(WorkRequest<MessageData, Metadata> file)
    {
        throw new NotImplementedException();
    }
}