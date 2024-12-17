namespace WorkManager.Job.POCO;

public class JobObject
{
    public string JobId { get; set; }
    public long CurrentTasks { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
}