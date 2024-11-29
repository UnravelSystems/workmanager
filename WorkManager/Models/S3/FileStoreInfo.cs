namespace WorkManager.Models.S3;

/// <summary>
///     Information about where the file was stored
/// </summary>
public class FileStoreInfo
{
    /// <summary>
    ///     Total size of the file that was stored
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    ///     Offset into whatever area we stored this at. Generally it will be zero unless we concatenated it to something.
    /// </summary>
    public long Offset { get; set; } = 0;

    /// <summary>
    ///     The path this was file was stored at (can be a key inside of a bucket, on disk file, whatever)
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    ///     The MD5 hash of the file we stored
    /// </summary>
    public string? MD5 { get; set; }
}