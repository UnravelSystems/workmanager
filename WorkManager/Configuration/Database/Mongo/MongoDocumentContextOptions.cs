namespace WorkManager.Configuration.Database.External;

[OptionsConfiguration(ServiceName = "document_context.mongo")]
public class MongoDocumentContextOptions
{
    public string Collection { get; set; }
    public string Database { get; set; }
}