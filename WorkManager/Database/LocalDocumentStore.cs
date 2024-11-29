using WorkManager.Configuration;
using WorkManager.Models;

namespace WorkManager.Database;

[ServiceConfiguration(ServiceName = "document_store", ServiceType = "local")]
public class LocalDocumentStore : IDocumentStore<Document<string, string>>
{
    public void AddDocument(string collectionName, Document<string, string> document)
    {
    }

    public void AddDocument(Document<string, string> document)
    {
    }

    public Document<string, string>? RetrieveDocument(string collectionName, string documentId)
    {
        return null;
    }

    public Document<string, string>? RetrieveDocument(string documentId)
    {
        return null;
    }
}