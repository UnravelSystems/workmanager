using WorkManager.Configuration.Database.External;
using WorkManager.Database.Mongo;

namespace WorkManager.Database;

public interface IDocumentStore<T>
{
    public void AddDocument(string collectionName, T document);
    public void AddDocument(T document);
    public T? RetrieveDocument(string collectionName, string documentId);
    public T? RetrieveDocument(string documentId);
}