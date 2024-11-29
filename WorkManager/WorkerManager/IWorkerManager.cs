namespace WorkManager.WorkerManager;

public interface IWorkerManager
{
}

/// <summary>
///     Interface for a WorkerManager, requires two methods to be implemented
/// </summary>
/// <typeparam name="TWorkItem">The class which represents the work item being worked on</typeparam>
public interface IWorkerManager<in TWorkItem> : IWorkerManager
{
    public void AddWorkItem(TWorkItem workItem);
    public void HandleWorkItem(TWorkItem workItem);
}