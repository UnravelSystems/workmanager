using WorkManager.Configuration.Datastore;
using WorkManager.Models.S3;

namespace WorkManager.Datastore;

public interface IDatastore: IDisposable
{
    public FileStoreInfo StoreFile(string storeArea, string storagePath, Stream dataStream);
    public FileStoreInfo StoreFile(string storeArea, string storagePath, string inFile);
    public Stream GetFile(string storeArea, string storagePath);
    public void GetFile(string storeArea, string storagePath, string outFile);
    public void Dispose();
}
public abstract class AbstractDatastore : IDatastore
{
    protected abstract FileStoreInfo Store(string storeArea, string storagePath, DatastoreStream datastoreStream);
    public abstract Stream GetFile(string storeArea, string storagePath);
    public abstract void GetFile(string storeArea, string storagePath, string outFile);

    /// <summary>
    /// StoreFile method which will automatically wrap the stream in a DatastoreStream for calculating size and MD5
    /// </summary>
    /// <param name="storeArea">Where the storage area is (Bucket, Drive, etc)</param>
    /// <param name="storagePath">Path within the storage area to store the file at</param>
    /// <param name="dataStream">Stream of data being stored</param>
    /// <returns>A FileStoreInfo object which contains the Offset (within the stored area), Size, MD5, and Path the data is stored at</returns>
    public FileStoreInfo StoreFile(string storeArea, string storagePath, Stream dataStream)
    {
        using (DatastoreStream datastoreStream = new DatastoreStream(dataStream))
        {
            FileStoreInfo storeInfo = Store(storeArea, storagePath, datastoreStream);
            storeInfo.MD5 = datastoreStream.MD5String;
            storeInfo.Size = datastoreStream.FileSize;
            return storeInfo;
        }
    }

    /// <summary>
    /// StoreFile method which will automatically wrap the stream in a DatastoreStream for calculating size and MD5
    /// </summary>
    /// <param name="storeArea">Where the storage area is (Bucket, Drive, etc)</param>
    /// <param name="storagePath">Path within the storage area to store the file at</param>
    /// <param name="inFile">What file to store</param>
    /// <returns>A FileStoreInfo object which contains the Offset (within the stored area), Size, MD5, and Path the data is stored at</returns>
    public FileStoreInfo StoreFile(string storeArea, string storagePath, string inFile)
    {
        using (FileStream fileStream = File.OpenRead(inFile))
        {
            return StoreFile(storeArea, storagePath, fileStream);
        }
    }

    public void Dispose()
    {
    }
}