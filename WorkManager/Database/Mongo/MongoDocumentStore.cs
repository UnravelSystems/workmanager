using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WorkManager.Configuration;
using WorkManager.Configuration.Database.External;
using WorkManager.MassTransit;
using WorkManager.Models;
using WorkManager.Models.Mongo;
using WorkManager.Models.Tree;

namespace WorkManager.Database.Mongo
{
    [ServiceConfiguration(ServiceName = "document_store", ServiceType = "mongo")]
    public class MongoDocumentStore : IDocumentStore<TreeDocument>
    {
        private readonly ILogger<MongoDocumentStore> _logger;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<MongoDocument>? _collection;

        public MongoDocumentStore(ILogger<MongoDocumentStore> logger, IMongoClient mongoClient,
            IOptions<MongoDocumentStoreOptions> options)
        {
            _logger = logger;
            MongoDocumentStoreOptions mongoOptions = options.Value;
            _database = mongoClient.GetDatabase(mongoOptions.Database);
            _collection = GetCollection(mongoOptions.Collection);
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
    }
}