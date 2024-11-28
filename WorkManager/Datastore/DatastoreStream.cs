using System.Security.Cryptography;

namespace WorkManager.Datastore;

/// <summary>
/// Used to wrap a data stream so that it computes the MD5 hash and length of a stream being stored
/// </summary>
public class DatastoreStream : Stream
{
    public string MD5String
    {
        get
        {
            _md5.TransformFinalBlock([], 0, 0);
            return BitConverter.ToString(_md5.Hash!).Replace("-", "");
        }
    }

    public long FileSize { get; set; }
    private readonly Stream _innerStream;
    private readonly MD5 _md5 = MD5.Create();

    public DatastoreStream(Stream innerStream)
    {
        _innerStream = innerStream;
        _md5.Initialize();
    }
    
    public override void Flush()
    {
        _innerStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = _innerStream.Read(buffer, offset, count);
        if (read <= 0)
        {
            return read;
        }
        FileSize += read;
        _md5.TransformBlock(buffer, offset, read, null, 0);
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => false;
    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
            _md5.Dispose();
        } 
        base.Dispose(disposing);
    }
}