namespace WorkManager.Models;

/// <summary>
/// Basic class for a work request, we need a job id, some kind of metadata and the data we are working on
/// IsCreated is used to determine if the job has already been created... this isn't necessary if we want to just call
/// the JobManager to check
/// </summary>
/// <typeparam name="T1">The metadata type</typeparam>
/// <typeparam name="T2">The data type</typeparam>
public class WorkRequest<T1, T2>
{
    /// <summary>
    /// The job id set by an external system or generated internally, needs to be unique
    /// </summary>
    public string? JobId { get; init; }
    
    /// <summary>
    /// The metadata object, this contains information about the work request provided to it. Also includes information
    /// about the data that needs to be passed through.
    /// EX: ResultBucket/ResultPrefix for something like S3, or the source of the data
    /// </summary>
    public T1 Metadata { get; init; }
    
    /// <summary>
    /// The actual data itself being worked on, this can be a list of objects if there are multiple items
    /// needing to be worked on. Or, a singular object which just points to the data and any other information required
    /// to get the data.
    /// EX: S3 would be the bucket and key of the data
    /// </summary>
    public T2 Data { get; init; }
    
    /// <summary>
    /// Whether the job has already been created. Isn't necessary.
    /// </summary>
    public bool IsCreated { get; init; }

    public override string ToString()
    {
        return $"{JobId} {Metadata} {IsCreated} {Data}";
    }
}