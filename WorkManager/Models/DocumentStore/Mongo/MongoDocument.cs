using MongoDB.Bson;
using WorkManager.Models.S3;
using WorkManager.Models.Tree;

namespace WorkManager.Models.Mongo;

public class MongoDocument : TreeDocument
{
    public MongoDocument(string jobId, string documentId, StringTreeNode data, Metadata metadata) : base(jobId,
        documentId, data, metadata)
    {
    }

    public MongoDocument(Document<StringTreeNode, Metadata> document) : base(document)
    {
    }

    public ObjectId Id { get; set; }
}