using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using WorkManager.Configuration;
using WorkManager.Models.S3;

namespace WorkManager.Datastore.S3;

[ServiceConfiguration(ServiceType = "S3", ServiceName = "datastore")]
public class S3Datastore : AbstractDatastore
{
    private readonly IAmazonS3 _amazonS3;
    private readonly ILogger<S3Datastore> _logger;

    public S3Datastore(ILogger<S3Datastore> logger, IAmazonS3 amazonS3)
    {
        _logger = logger;
        _amazonS3 = amazonS3;
    }

    protected override FileStoreInfo Store(string bucket, string key, DatastoreStream inStream)
    {
        TransferUtility fileTransferUtility =
            new TransferUtility(_amazonS3);
        TransferUtilityUploadRequest req = new TransferUtilityUploadRequest
        {
            AutoCloseStream = false,
            BucketName = bucket,
            Key = key,
            InputStream = inStream
        };

        fileTransferUtility.Upload(req);

        return new FileStoreInfo
        {
            Offset = 0,
            Path = $"{bucket}:/{key}"
        };
    }

    public override Stream GetFile(string bucket, string key)
    {
        throw new NotImplementedException();
    }

    public override void GetFile(string bucket, string key, string outFile)
    {
        throw new NotImplementedException();
    }
}