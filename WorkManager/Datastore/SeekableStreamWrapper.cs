namespace WorkManager.Datastore;

public class SeekableStreamWrapper : Stream
{
    private readonly Stream _innerStream;

    private FileStream? _tempFileStream;
    private FileStream TempFileStream
    {
        get
        {
            if (_tempFileStream == null)
            {
                string tempFile = Path.GetTempFileName();
                _tempFileStream = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite);
            }
            
            return _tempFileStream;
        }
    }

    private long _totalLength;
    
    private const int BufferSize = 8192;
    
    public SeekableStreamWrapper(Stream stream, long length)
    {
        _innerStream = stream;
        _totalLength = length;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _totalLength;

    public override long Position
    {
        get => TempFileStream.Position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush()
    {
        TempFileStream.Flush();
    }

    /// <summary>
    /// Reads data from the streams, ensures any data is read from the FileStream and if needed from the inner stream
    ///
    /// TODO: Looking into optimizing by just reading from a buffer if we are within the bounds
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        // We are outside of the file stream so read from the inner stream until data is available
        if (TempFileStream.Position >= TempFileStream.Length)
        {
            return ReadFromInnerStream(buffer, offset, count);
        }
        
        // Read as much as we can into the file read
        int fileRead = TempFileStream.Read(buffer, offset, count);
        if (fileRead >= count)
        {
            // We got all we needed
            return fileRead;
        }
        
        // Read rest from the inner stream
        int remaining = count - fileRead;
        return fileRead + ReadFromInnerStream(buffer, offset + fileRead, remaining);
    }

    private int ReadFromInnerStream(byte[] buffer, int offset, int count)
    {
        int bytesRead = _innerStream.Read(buffer, offset, count);
        TempFileStream.Write(buffer, offset, bytesRead);
        return bytesRead;
    }

    /// <summary>
    /// Seeks to a position within the streams, we calculate where the seek will result, and then ensure we have
    /// the data available
    /// </summary>
    /// <param name="offset">Offset based on the SeekOrigin type (End should be negative)</param>
    /// <param name="origin"></param>
    /// <returns>Position seeked to</returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
        long targetPosition = offset;
        switch (origin)
        {
            case SeekOrigin.Begin:
                targetPosition = offset;
                break;
            case SeekOrigin.Current:
                targetPosition += TempFileStream.Position; // Start where the FileStream currently is
                break;
            case SeekOrigin.End:
                if (offset > 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), "Offset should be negative when using SEEK_END");
                }
                targetPosition = Length + offset; // Start from the end and read back
                break;
        }

        // Validate we are within valid positioning
        if (targetPosition < 0 || targetPosition > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset should result in a position between 0 and Length");
        }
        
        // Ensure we have the data available for this position, this might incur heavy read costs
        EnsureDataAvailable(targetPosition);
        return TempFileStream.Seek(targetPosition, SeekOrigin.Begin);
    }
    
    /// <summary>
    /// Ensures that the data is available within the temporary file to be read
    /// </summary>
    /// <param name="position"></param>
    private void EnsureDataAvailable(long position)
    {
        // We good
        if (position < TempFileStream.Length)
        {
            return;
        }
        
        byte[] buffer = new byte[BufferSize];
            
        // Read up to the position and store it in the stream, but also ensure we aren't going over the total
        //  stream length
        while (TempFileStream.Length < position && _innerStream.Position < Length)
        {
            int bytesRead = _innerStream.Read(buffer, 0, BufferSize);
            TempFileStream.Write(buffer, 0, bytesRead);
        }
    }

    /// <summary>
    /// This is a fake dispose that is just meant to reset the stream pointer
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        Seek(0, SeekOrigin.Begin);
    }

    /// <summary>
    /// This is the real dispose, we call this directly from WorkManager specific tasking.
    /// This avoids another function from calling dispose and destroying the streams
    /// </summary>
    /// <param name="disposing"></param>
    public void DisposeForReal(bool disposing=true)
    {
        if (disposing)
        {
            _innerStream.Dispose();

            if (_tempFileStream != null)
            {
                string fileName = _tempFileStream.Name;
                _tempFileStream.Dispose();
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                _tempFileStream = null;
            }
        }
        base.Dispose(disposing);
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}