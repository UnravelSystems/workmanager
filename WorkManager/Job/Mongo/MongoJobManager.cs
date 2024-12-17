using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using WorkManager.Configuration;
using WorkManager.Configuration.Job;
using WorkManager.Job.POCO;

namespace WorkManager.Job.Mongo;

public class MongoJobObject : JobObject
{
    public ObjectId _id { get; set; }
}

[ServiceConfiguration(ServiceType = "mongo", ServiceName = "job_manager")]
public class MongoJobManager : IJobManager
{
    private readonly IMongoCollection<MongoJobObject> _collection;
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoJobManager> _logger;

    private readonly FindOneAndUpdateOptions<MongoJobObject> _updateOptions = new()
    {
        IsUpsert = false,
        ReturnDocument = ReturnDocument.After
    };

    private readonly FindOneAndUpdateOptions<MongoJobObject> _upsertOptions = new()
    {
        IsUpsert = true,
        ReturnDocument = ReturnDocument.After
    };

    public MongoJobManager(IMongoClient client, IOptions<MongoJobManagerOptions> mongoJobManagerOptions,
        ILogger<MongoJobManager> logger)
    {
        _database = client.GetDatabase(mongoJobManagerOptions.Value.Database);
        _collection = _database.GetCollection<MongoJobObject>(mongoJobManagerOptions.Value.Collection);
        _logger = logger;
    }

    public long AddTask(string jobId, string taskId)
    {
        JobObject? foundJob = _collection.FindOneAndUpdate(
            Builders<MongoJobObject>.Filter.Where(j => j.JobId == jobId),
            Builders<MongoJobObject>.Update
                .SetOnInsert(j => j.StartTime, DateTime.UtcNow)
                .SetOnInsert(j => j.JobId, jobId)
                .Set(j => j.LastUpdateTime, DateTime.UtcNow)
                .Inc("CurrentTasks", 1),
            _upsertOptions);
        _logger.LogInformation($"Incrementing task {jobId} | current_tasks: {foundJob.CurrentTasks}");
        return foundJob.CurrentTasks;
    }

    public long RemoveTask(string jobId, string taskId)
    {
        _logger.LogDebug($"Decrementing task {jobId}");
        JobObject? foundJob = _collection.FindOneAndUpdate(
            Builders<MongoJobObject>.Filter.Where(j => j.JobId == jobId),
            Builders<MongoJobObject>.Update
                .Set(j => j.LastUpdateTime, DateTime.UtcNow)
                .Inc("CurrentTasks", -1),
            _updateOptions);

        if (foundJob == null)
        {
            throw new Exception($"Job {jobId} not found");
        }

        _logger.LogInformation($"Decrementing task {jobId} | current_tasks: {foundJob.CurrentTasks}");
        return foundJob.CurrentTasks;
    }

    public bool IsJobFinished(string jobId)
    {
        JobObject? job = _collection.FindSync(Builders<MongoJobObject>.Filter.Where(j => j.JobId == jobId)).FirstOrDefault();
        if (job == null)
        {
            return true;
        }

        return job.CurrentTasks == 0;
    }

    public bool FinishJob(string jobId)
    {
        _logger.LogInformation($"Removing task {jobId}");
        DeleteResult? result = _collection.DeleteOne(Builders<MongoJobObject>.Filter.Where(j => j.JobId == jobId));
        return result.DeletedCount == 1;
    }
}