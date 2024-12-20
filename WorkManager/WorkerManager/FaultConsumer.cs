﻿using MassTransit;
using Microsoft.Extensions.Logging;
using WorkManager.Job;
using WorkManager.Models;
using WorkManager.Models.S3;

namespace WorkManager.WorkerManager;

/// <summary>
///     A fault consumer, this gets triggered when the main consumer critically fails
/// </summary>
public class FaultConsumer : IConsumer<Fault<WorkRequest<MessageData, Metadata>>>
{
    private readonly IJobManager _jobManager;
    private readonly ILogger<MessageConsumer> _logger;

    public FaultConsumer(ILogger<MessageConsumer> logger, IJobManager jobManager)
    {
        _logger = logger;
        _jobManager = jobManager;
    }

    public async Task Consume(ConsumeContext<Fault<WorkRequest<MessageData, Metadata>>> context)
    {
        _logger.LogInformation("Fault consumer received: {@context}", context);
        WorkRequest<MessageData, Metadata> workRequest = context.Message.Message;
        long activeJobs = _jobManager.RemoveTask(workRequest.JobId, null);
        if (_jobManager.IsJobFinished(workRequest.JobId))
        {
            _logger.LogInformation(
                $"Job {workRequest.JobId} has finished through fault: {_jobManager.FinishJob(workRequest.JobId)}");
        }
    }
}