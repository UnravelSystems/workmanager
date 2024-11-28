using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using WorkManager.Configuration;

namespace WorkManager.Job;

[ServiceConfiguration(ServiceType = "local", ServiceName = "job_manager")]
public class LocalJobManager : IJobManager
{
    private ConcurrentDictionary<string, long> _jobs;
    private readonly ILogger<LocalJobManager> _logger;
    public LocalJobManager(ILogger<LocalJobManager> logger)
    {
        _logger = logger;
        _jobs = new ConcurrentDictionary<string, long>();
    }

    public long AddTask(string jobId, string taskId)
    {
        _logger.LogInformation($"Incrementing task {jobId} {_jobs.GetValueOrDefault(jobId, 0)}");
        return _jobs.AddOrUpdate(jobId, 1, (k, v) => v + 1);
    }

    public long RemoveTask(string jobId, string taskId)
    {
        _logger.LogInformation($"Decrementing task {jobId} {_jobs[jobId]}");
        if (!_jobs.ContainsKey(jobId))
        {
            throw new Exception($"Job {jobId} not found");
        }
        return _jobs.AddOrUpdate(jobId, 1, (k, v) => v - 1);
    }

    public bool IsJobFinished(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var jobCount))
        {
            throw new Exception($"Job {jobId} not found");
        }
        
        return jobCount == 0;
    }

    public bool FinishJob(string jobId)
    {
        return _jobs.TryRemove(jobId, out _);
    }
}