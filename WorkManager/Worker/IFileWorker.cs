using WorkManager.MassTransit;

namespace WorkManager.Worker;

/// <summary>
/// A worker interface that deals with handling some kind of file object
/// </summary>
/// <typeparam name="T">An object which represents a file</typeparam>
public interface IFileWorker<T> : IWorker<T>
{
    /// <summary>
    /// Process a file like object
    /// </summary>
    /// <param name="file"></param>
    public void ProcessFile(T file);
}