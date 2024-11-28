namespace WorkManager.WorkerManager;

public interface IWorkerManager {}

/// <summary>
/// Interface for a WorkerManager, requires two methods to be implemented
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IWorkerManager<T> : IWorkerManager
{
    public void AddWorkItem(T workItem);
    public void HandleWorkItem(T workItem);
}