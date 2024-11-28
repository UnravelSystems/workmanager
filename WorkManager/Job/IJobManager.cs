namespace WorkManager.Job;

public interface IJobManager
{
    public long AddTask(string jobId, string taskId);
    public long RemoveTask(string jobId, string taskId);
    public bool IsJobFinished(string jobId);
    public bool FinishJob(string jobId);
}