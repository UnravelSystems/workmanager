namespace WorkManager.Models;

/// <summary>
/// Basis for a document being stored in a document store. This is what is deemed as the basic requirement for working.
/// </summary>
/// <typeparam name="T1">The data type for the actual data being stored</typeparam>
/// <typeparam name="T2">Metadata type for storing information about the source of the data</typeparam>
public class Document<T1, T2>
{
    /// <summary>
    /// What job this data belongs to
    /// </summary>
    public string JobId { get; set; }
    
    /// <summary>
    /// The id for the document being stored within the system... this might not be needed at this level
    /// </summary>
    public string DocumentId { get; set; }
    
    /// <summary>
    /// The data being stored, this is the actual document that we care about
    /// </summary>
    public T1 Data { get; set; }
    
    /// <summary>
    /// Metadata about the document being stored, stuff like the source of the data, or any caveats like a timestamp
    /// </summary>
    public T2 Metadata { get; set; }

    public Document(string jobId, string documentId, T1 data, T2 metadata)
    {
        JobId = jobId;
        DocumentId = documentId;
        Data = data;
        Metadata = metadata;
    }

    public Document(Document<T1, T2> document)
    {
        DocumentId = document.DocumentId;
        Data = document.Data;
        JobId = document.JobId;
        Metadata = document.Metadata;
    }
}