

using WorkManager.Models.S3;

namespace WorkManager.Models.Tree;

public class TreeDocument : Document<StringTreeNode, Metadata>
{
    public TreeDocument(string jobId, string documentId, StringTreeNode data, Metadata metadata) : base(jobId,
        documentId, data, metadata)
    {
    }

    public TreeDocument(Document<StringTreeNode, Metadata> document) : base(document)
    {
    }
}