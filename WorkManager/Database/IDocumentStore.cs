namespace WorkManager.Database;

/// <summary>
///     Interface for a document store, can be implemented for different types of document storage
/// </summary>
/// <typeparam name="TDocument">This is the document type</typeparam>
public interface IDocumentStore<TDocument>
{
    /// <summary>
    ///     Adds a document to a specific collection
    /// </summary>
    /// <param name="collectionName">Name of the collection to add to</param>
    /// <param name="document">Document being stored</param>
    public void AddDocument(string collectionName, TDocument document);

    /// <summary>
    ///     Adds a document to the default collection
    /// </summary>
    /// <param name="document">Document being stored</param>
    public void AddDocument(TDocument document);

    /// <summary>
    ///     Retrieves a document from a specific collection based on the document id
    /// </summary>
    /// <param name="collectionName">The collection to retrieve the document from</param>
    /// <param name="documentId">The identifier of the document</param>
    /// <returns>An object of TDocument if one was found</returns>
    public TDocument? RetrieveDocument(string collectionName, string documentId);

    /// <summary>
    ///     Retrieves a document from the default collection
    /// </summary>
    /// <param name="documentId">The identifier for the document</param>
    /// <returns>An object of TDocument if one was found</returns>
    public TDocument? RetrieveDocument(string documentId);
}