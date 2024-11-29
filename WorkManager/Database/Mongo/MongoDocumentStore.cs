using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WorkManager.Configuration;
using WorkManager.Configuration.Database.External;
using WorkManager.Models.Mongo;
using WorkManager.Models.Tree;

namespace WorkManager.Database.Mongo;

/// <summary>
///     An implementation of a mongo document store which takes a TreeDocument.
/// </summary>
[ServiceConfiguration(ServiceName = "document_store", ServiceType = "mongo")]
public class MongoDocumentStore : IDocumentStore<TreeDocument>
{
    private readonly IMongoCollection<MongoDocument>? _collection;
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDocumentStore> _logger;

    /// <summary>
    ///     Constructor which takes a logger, IMongoClient and autoconfigured options
    /// </summary>
    /// <param name="mongoClient">A mongo client</param>
    /// <param name="options">An options class containing information about the default database and collection</param>
    /// <param name="logger">A logger instance</param>
    public MongoDocumentStore(IMongoClient mongoClient, IOptions<MongoDocumentStoreOptions> options,
        ILogger<MongoDocumentStore> logger)
    {
        _logger = logger;
        MongoDocumentStoreOptions mongoOptions = options.Value;
        _database = mongoClient.GetDatabase(mongoOptions.Database);
        _collection = GetCollection(mongoOptions.Collection);
    }

    /// <summary>
    ///     Adds a TreeDocument to a specific collection
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="document"></param>
    public void AddDocument(string collectionName, TreeDocument document)
    {
        IMongoCollection<MongoDocument>? collection = GetCollection(collectionName);
        collection?.InsertOne(new MongoDocument(document));
    }

    public void AddDocument(TreeDocument document)
    {
        _collection!.InsertOne(new MongoDocument(document));
    }

    public TreeDocument RetrieveDocument(string documentId)
    {
        return _collection
            .Find(Builders<MongoDocument>.Filter.Eq("DocumentId", documentId)).First();
    }

    public TreeDocument? RetrieveDocument(string collectionName, string documentId)
    {
        IMongoCollection<MongoDocument>? collection = GetCollection(collectionName);
        return collection
            ?.Find(Builders<MongoDocument>.Filter.Eq("DocumentId", documentId)).First();
    }

    private IMongoCollection<MongoDocument>? GetCollection(string collectionName)
    {
        if (string.IsNullOrEmpty(collectionName))
        {
            return null;
        }

        CreateCollectionIfNotExists(collectionName);
        return _database.GetCollection<MongoDocument>(collectionName);
    }

    private void CreateCollectionIfNotExists(string collectionName)
    {
        _database.CreateCollection(collectionName);
    }
}