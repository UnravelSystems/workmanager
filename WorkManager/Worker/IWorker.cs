using WorkManager.MassTransit;
using WorkManager.WorkerManager;

namespace WorkManager.Worker;

/// <summary>
/// Interface for a generic worker, requires three methods to be implemented
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IWorker<T>
{
    /// <summary>
    /// Sets a worker manager, this is required to avoid a circular dependency problem between:
    ///     WorkerManager -> Worker -> WorkerManager
    /// </summary>
    /// <param name="manager">The worker manager which will be called to add more work back to the system</param>
    public void SetWorkerManager(IWorkerManager manager);

    /// <summary>
    /// Whether this worker can accept the request
    /// </summary>
    /// <param name="request">The request currently being processed</param>
    /// <returns>True if the work can be done via this worker</returns>
    public bool Accepts(T request);
    
    /// <summary>
    /// Processes a current work item, this method will potentially call back to the WorkerManager to add more
    /// work back to the system if it needs to
    /// </summary>
    /// <param name="request">The request currently being processed</param>
    public void Process(T request);
}