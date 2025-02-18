﻿namespace WorkManager.Datastore;

/// <summary>
///     A Stream wrapper used when a Seekable stream is required or seeking on the original stream would be too much
///     overhead
/// </summary>
public class SeekableStreamWrapper : Stream
{
    private const int OneMbBufferSize = 1024 * 1024;

    private readonly Stream _innerStream;
    private readonly long _totalLength;

    private string? _tempFilePath;
    private BufferedStream? _tempFileStream;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="stream">The stream we are wrapping</param>
    /// <param name="length">The length of the streamed data</param>
    /// <param name="bufferSize">The buffer size, defaults to 1 MB</param>
    public SeekableStreamWrapper(Stream stream, long length, int bufferSize = OneMbBufferSize)
    {
        _innerStream = stream;
        _totalLength = length;
        BufferSize = bufferSize;
    }

    private int BufferSize { get; }

    /// <summary>
    ///     Creates a temporary file stream to store data on disk
    /// </summary>
    private BufferedStream TempFileStream
    {
        get
        {
            if (_tempFileStream == null)
            {
                _tempFilePath = Path.GetTempFileName();
                var fileStream = new FileStream(_tempFilePath, FileMode.Create, FileAccess.ReadWrite);
                _tempFileStream = new BufferedStream(fileStream, BufferSize);
            }

            return _tempFileStream;
        }
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
    ///     Reads data from the streams, ensures any data is read from the FileStream and if needed from the inner stream
    /// </summary>
    /// <param name="buffer">The buffer to fill</param>
    /// <param name="offset">Offset into the buffer to read</param>
    /// <param name="count">Number of bytes to read</param>
    /// <returns>The number of bytes read</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Must be greater than or equal to zero.");
        }
        
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Must be greater than or equal to zero.");
        }
        
        if (count > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be less than or equal to buffer.Length");
        }

        if (offset + count > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count + offset, "Count with offset must be less than or equal to buffer.Length");
        }
        
        // We are outside of the file stream so read from the inner stream until data is available
        if (TempFileStream.Position >= TempFileStream.Length)
        {
            return ReadFromInnerStream(buffer, offset, count);
        }

        // Read as much as we can into the file read
        var fileRead = TempFileStream.Read(buffer, offset, count);
        if (fileRead >= count)
        {
            // We got all we needed
            return fileRead;
        }

        // Read rest from the inner stream
        var remaining = count - fileRead;
        return fileRead + ReadFromInnerStream(buffer, offset + fileRead, remaining);
    }

    /// <summary>
    ///     Reads data from the wrapped stream when needed, writes the data to disk
    /// </summary>
    /// <param name="buffer">The buffer we are filling</param>
    /// <param name="offset">Offset within the buffer to put the data</param>
    /// <param name="count">Number of bytes to fill</param>
    /// <returns></returns>
    private int ReadFromInnerStream(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);
        TempFileStream.Write(buffer, offset, bytesRead);
        return bytesRead;
    }

    /// <summary>
    ///     Seeks to a position within the streams, we calculate where the seek will result, and then ensure we have
    ///     the data available
    /// </summary>
    /// <param name="offset">Offset based on the SeekOrigin type (End should be negative)</param>
    /// <param name="origin"></param>
    /// <returns>Position seeked to</returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
        var targetPosition = offset;
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
                    throw new ArgumentOutOfRangeException(nameof(offset),
                        "Offset should be negative when using SEEK_END");
                }

                targetPosition = Length + offset; // Start from the end and read back
                break;
        }

        // Validate we are within valid positioning
        if (targetPosition < 0 || targetPosition > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset),
                "Offset should result in a position between 0 and Length");
        }

        // Ensure we have the data available for this position, this might incur heavy read costs
        EnsureDataAvailable(targetPosition);

        var currentPosition = TempFileStream.Position;

        // We are currently already at the correct position
        if (targetPosition == currentPosition)
        {
            return targetPosition;
        }

        // Calculate the distance from the current position to the target to determine if it is better to seek
        //  from current or begin
        var distanceFromTarget = currentPosition - targetPosition;

        // It is better to seek from the beginning because the target position is closer to 0
        if (Math.Abs(distanceFromTarget) > targetPosition)
        {
            return TempFileStream.Seek(targetPosition, SeekOrigin.Begin);
        }

        // Seek back to the target position from current
        return TempFileStream.Seek(-distanceFromTarget, SeekOrigin.Current);
    }

    /// <summary>
    ///     Ensures that the data is available within the temporary file to be read
    /// </summary>
    /// <param name="position"></param>
    private void EnsureDataAvailable(long position)
    {
        // We good
        if (position < TempFileStream.Length)
        {
            return;
        }

        var buffer = new byte[BufferSize];

        // Read up to the position and store it in the stream, but also ensure we aren't going over the total
        //  stream length
        while (TempFileStream.Length < position && _innerStream.Position < Length)
        {
            var bytesRead = _innerStream.Read(buffer, 0, BufferSize);
            TempFileStream.Write(buffer, 0, bytesRead);
        }
    }

    /// <summary>
    ///     This is a fake dispose that is just meant to reset the stream pointer
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        Seek(0, SeekOrigin.Begin);
    }

    /// <summary>
    ///     This is the real dispose, we call this directly from WorkManager specific tasking.
    ///     This avoids another function from calling dispose and destroying the streams
    /// </summary>
    /// <param name="disposing"></param>
    public void DisposeForReal(bool disposing = true)
    {
        if (disposing)
        {
            _innerStream.Dispose();

            if (_tempFileStream != null)
            {
                var fileName = _tempFilePath;
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