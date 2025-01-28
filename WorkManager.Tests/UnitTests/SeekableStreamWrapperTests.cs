using System.Reflection;
using WorkManager.Datastore;

namespace WorkManager.Tests.UnitTests;

public class SeekableStreamWrapperTests
{
    private SeekableStreamWrapper _seekableStreamWrapper;
    private MemoryStream _memoryStream;
    private readonly byte[] _testData;

    public SeekableStreamWrapperTests()
    {
        string largeTestString = new string('A', 8192) + new string('B', 8192) + new string('C', 8192);
        _testData = System.Text.Encoding.UTF8.GetBytes(largeTestString);
    }

    [SetUp]
    public void Setup()
    {
        _memoryStream = new MemoryStream(_testData);
        _seekableStreamWrapper = new SeekableStreamWrapper(_memoryStream, _testData.Length, 8192);
    }

    [TearDown]
    public void TearDown()
    {
        // This gets it to not complain
        _seekableStreamWrapper.Dispose();
        _seekableStreamWrapper.DisposeForReal();
        _memoryStream.Dispose();
    }
    
    private BufferedStream GetTempFileStream(SeekableStreamWrapper seekableStreamWrapper)
    {
        var tempFileStreamProperty = typeof(SeekableStreamWrapper).GetProperty("TempFileStream", BindingFlags.NonPublic | BindingFlags.Instance);
        if (tempFileStreamProperty == null)
        {
            throw new InvalidOperationException("TempFileStream property not found");
        }
        return (BufferedStream)tempFileStreamProperty.GetValue(seekableStreamWrapper);
    }
    
    private string GetTempFilePath(SeekableStreamWrapper seekableStreamWrapper)
    {
        var tempFileStreamProperty = typeof(SeekableStreamWrapper).GetField("_tempFilePath", BindingFlags.NonPublic | BindingFlags.Instance);
        if (tempFileStreamProperty == null)
        {
            throw new InvalidOperationException("TempFileStream property not found");
        }
        return (string)tempFileStreamProperty.GetValue(seekableStreamWrapper);
    }
    
    [TestCase(10, Description = "Seek within the bounds of the stream")]
    [TestCase(24576, Description = "Seek to the end of the stream")]
    public void Seek_ShouldWriteDataToTempFile(long seekPosition)
    {
        BufferedStream fileStream = GetTempFileStream(_seekableStreamWrapper);
        long initialTempFileLength = fileStream.Length;

        _seekableStreamWrapper.Seek(seekPosition, SeekOrigin.Begin);
        
        Assert.That(fileStream.Length, Is.GreaterThan(initialTempFileLength));
    }
    
    [TestCase(0, SeekOrigin.Begin, 8192, 8192, 0, Description = "Read and validate data from beginning of the stream")]
    [TestCase(8192, SeekOrigin.Begin, 8192, 8192, 8192, Description = "Read and validate data from 8192 bytes into the stream")]
    [TestCase(16384, SeekOrigin.Begin, 8192, 8192, 16384, Description = "Read and validate data from 16384 bytes into the stream")]
    [TestCase(0, SeekOrigin.End, 8192, 0, 24576, Description = "Read and validate no bytes read when stream is at end")]
    [TestCase(-8192, SeekOrigin.End, 8192, 8192, 16384, Description = "Read and validate 8192 bytes read when seeked -8192 from end")]
    [TestCase(10000, SeekOrigin.Begin, 8192, 8192, 10000, Description = "Read and validate data from 10000 bytes into the stream")] 
    [TestCase(24576, SeekOrigin.Begin, 8192, 0, 24576, Description = "Validate no data was read when seeked to end of the stream")]
    [TestCase(16384, SeekOrigin.Current, 100000, 8192, 16384, Description = "Validate data is read from within the bounds of the stream")]
    [TestCase(0, SeekOrigin.Begin, 10000, 10000, 0, Description = "Validate data is read correctly when buffer is larger than internal buffer")]
    public void SeekableStream_Read_ValidateData(long position, SeekOrigin origin, int count, int expectedLength, long expectedPosition)
    {
        _seekableStreamWrapper.Seek(position, origin);
        byte[] buffer = new byte[count];
        int bytesRead = _seekableStreamWrapper.Read(buffer, 0, count);

        Assert.That(bytesRead, Is.EqualTo(expectedLength));
        Assert.That(_seekableStreamWrapper.Position - bytesRead, Is.EqualTo(expectedPosition));

        byte[] expectedData = new byte[expectedLength];
        Array.Copy(_testData, expectedPosition, expectedData, 0, expectedLength);

        byte[] actualData = new byte[bytesRead];
        Array.Copy(buffer, actualData, bytesRead);

        Assert.That(actualData, Is.EqualTo(expectedData));
    }

    [TestCase(0, SeekOrigin.Begin)]
    [TestCase(10000, SeekOrigin.Begin)]
    [TestCase(10000, SeekOrigin.Current)]
    [TestCase(-10000, SeekOrigin.End)]
    [TestCase(0, SeekOrigin.End)]
    public void SeekableStream_Seek_ValidPositions(long offset, SeekOrigin origin)
    {
        long expectedPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _seekableStreamWrapper.Position + offset,
            SeekOrigin.End => _seekableStreamWrapper.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };

        Assert.DoesNotThrow(() => _seekableStreamWrapper.Seek(offset, origin));
        Assert.That(_seekableStreamWrapper.Position, Is.EqualTo(expectedPosition));
    }

    [TestCase(-1, SeekOrigin.Begin)]
    [TestCase(30000, SeekOrigin.Begin)]
    [TestCase(-10000, SeekOrigin.Current)]
    [TestCase(1000000, SeekOrigin.End)]
    [TestCase(10000, SeekOrigin.End)]
    public void SeekableStream_Seek_InvalidPositions(long offset, SeekOrigin origin)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _seekableStreamWrapper.Seek(offset, origin));
    }
    
    [TestCase(0, 10000, Description = "Count is larger than internal buffer")]
    [TestCase(600, 500, Description = "Count + Offset is larger than internal buffer")]
    [TestCase(-1, 10000, Description = "Offset is less than 0")]
    [TestCase(600, -1, Description = "Count is less than 0")]
    public void SeekableStream_Read_InvalidArguments(int offset, int count)
    {
        byte[] buffer = new byte[1000];
        Assert.Throws<ArgumentOutOfRangeException>(() => _seekableStreamWrapper.Read(buffer, offset, count));
    }

    [Test]
    public void SeekableStream_ReadThenSeekToZero_InnerStreamPositionIsUnchanged()
    {
        Stream innerStream = GetInnerStream(_seekableStreamWrapper)!;
        byte[] buffer = new byte[8192];
        int bytesRead = _seekableStreamWrapper.Read(buffer, 0, 8192);
        Assert.Multiple(() =>
        {
            Assert.That(bytesRead, Is.EqualTo(8192));
            Assert.That(_seekableStreamWrapper.Position, Is.EqualTo(8192));
            Assert.That(innerStream.Position, Is.EqualTo(8192));
        });

        _seekableStreamWrapper.Seek(0, SeekOrigin.Begin);
        Assert.Multiple(() =>
        {
            Assert.That(_seekableStreamWrapper.Position, Is.EqualTo(0));
            Assert.That(innerStream.Position, Is.EqualTo(8192));
        });

        buffer = new byte[8192];
        bytesRead = _seekableStreamWrapper.Read(buffer, 0, 4096);
        Assert.Multiple(() =>
        {
            Assert.That(bytesRead, Is.EqualTo(4096));
            Assert.That(_seekableStreamWrapper.Position, Is.EqualTo(4096));
            Assert.That(innerStream.Position, Is.EqualTo(8192));
        });
    }
    
    [Test]
    public void Seek_WithinBounds_ShouldSeekFromCurrentOrBeginDependingOnDistance()
    {
        _seekableStreamWrapper.Seek(1000, SeekOrigin.Begin); // Set initial position

        // Case where seeking from Begin is closer
        long position = _seekableStreamWrapper.Seek(100, SeekOrigin.Begin);
        Assert.That(position, Is.EqualTo(100));
        Assert.That(_seekableStreamWrapper.Position, Is.EqualTo(100));

        // Case where seeking from Current is closer
        _seekableStreamWrapper.Seek(700, SeekOrigin.Begin);
        position = _seekableStreamWrapper.Seek(600, SeekOrigin.Begin);
        Assert.That(position, Is.EqualTo(600));
        Assert.That(_seekableStreamWrapper.Position, Is.EqualTo(600));

        // Case where seeking from Current is after the current position
        _seekableStreamWrapper.Seek(500, SeekOrigin.Begin);
        position = _seekableStreamWrapper.Seek(600, SeekOrigin.Begin);
        Assert.That(position, Is.EqualTo(600));
        Assert.That(_seekableStreamWrapper.Position, Is.EqualTo(600));
    }
    
    [Test]
    public void SetPosition_WithinBounds_ShouldSeekFromCurrentOrBeginDependingOnDistance()
    {
        _seekableStreamWrapper.Position = 1000; // Set initial position

        // Case where seeking from Begin is closer
        long position = _seekableStreamWrapper.Position = 100;
        Assert.That(position, Is.EqualTo(100));
        Assert.That(_seekableStreamWrapper.Position, Is.EqualTo(100));

        // Case where seeking from Current is closer
        _seekableStreamWrapper.Position = 700;
        position = _seekableStreamWrapper.Position = 600;
        Assert.That(position, Is.EqualTo(600));
        Assert.That(_seekableStreamWrapper.Position, Is.EqualTo(600));

        // Case where seeking from Current is after the current position
        _seekableStreamWrapper.Position = 500;
        position = _seekableStreamWrapper.Position = 600;;
        Assert.That(position, Is.EqualTo(600));
        Assert.That(_seekableStreamWrapper.Position, Is.EqualTo(600));
    }

    [Test]
    public void Dispose_ShouldDeleteTemporaryFile()
    {
        _seekableStreamWrapper.Seek(10000, SeekOrigin.Begin);
        string tempFilePath = GetTempFilePath(_seekableStreamWrapper);
        Assert.That(File.Exists(tempFilePath), Is.True);

        _seekableStreamWrapper.Dispose();
        Assert.That(File.Exists(tempFilePath), Is.True);
        
        _seekableStreamWrapper.DisposeForReal();
        Assert.That(File.Exists(tempFilePath), Is.False);
    }

    private Stream? GetInnerStream(SeekableStreamWrapper streamWrapper)
    {
        // Reflection to access private _tempFileStream field and get the temporary file path
        var tempFileStreamField = typeof(SeekableStreamWrapper).GetField("_innerStream",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (tempFileStreamField != null)
        {
            return (Stream)tempFileStreamField.GetValue(streamWrapper);
        }

        return null;
    }
}