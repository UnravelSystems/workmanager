using Microsoft.EntityFrameworkCore;

namespace WorkManager.Models;

/// <summary>
///     Basis for a document being stored in a document store. This is what is deemed as the basic requirement for working.
///     Decorating the class with [PrimaryKey(...)] helps with EFCore ensuring the Primary Key is known before conventions occur.
/// </summary>
/// <typeparam name="TData">The data type for the actual data being stored</typeparam>
/// <typeparam name="TMetadata">Metadata type for storing information about the source of the data</typeparam>
[PrimaryKey("DocumentId")]
public class Document<TData, TMetadata>
{
    public Document() {}
    
    public Document(string jobId, string documentId, TData data, TMetadata metadata)
    {
        JobId = jobId;
        DocumentId = documentId;
        Data = data;
        Metadata = metadata;
    }

    public Document(Document<TData, TMetadata> document)
    {
        DocumentId = document.DocumentId;
        Data = document.Data;
        JobId = document.JobId;
        Metadata = document.Metadata;
    }

    /// <summary>
    ///     What job this data belongs to
    /// </summary>
    public string JobId { get; set; }

    /// <summary>
    ///     The id for the document being stored within the system... this might not be needed at this level
    /// </summary>
    public string DocumentId { get; set; }

    /// <summary>
    ///     The data being stored, this is the actual document that we care about
    /// </summary>
    public TData Data { get; set; }

    /// <summary>
    ///     Metadata about the document being stored, stuff like the source of the data, or any caveats like a timestamp
    /// </summary>
    public TMetadata Metadata { get; set; }
}