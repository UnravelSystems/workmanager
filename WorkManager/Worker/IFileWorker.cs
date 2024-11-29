namespace WorkManager.Worker;

/// <summary>
///     A worker interface that deals with handling some kind of file object
/// </summary>
/// <typeparam name="TWorkItem">An object which represents a file based work item</typeparam>
public interface IFileWorker<in TWorkItem> : IWorker<TWorkItem>
{
    /// <summary>
    ///     Process a file like object
    /// </summary>
    /// <param name="file"></param>
    public void ProcessFile(TWorkItem file);
}