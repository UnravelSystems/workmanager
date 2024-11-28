namespace WorkManager.Configuration.Database.External;

[OptionsConfiguration(ServiceName = "document_store.mongo")]
public class MongoDocumentStoreOptions
{
    public string Collection { get; set; }
    public string Database { get; set; }
}