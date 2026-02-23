namespace PeSharp.Tests;

public class PeReaderTests
{
    [Fact]
    public void Constructor_ByteArray_SetsLengthCorrectly()
    {
        var data = new byte[100];
        using var reader = new PeReader(data);
        Assert.Equal(100, reader.Length);
    }

    [Fact]
    public void Constructor_ByteArray_Empty_SetsLengthZero()
    {
        using var reader = new PeReader(Array.Empty<byte>());
        Assert.Equal(0, reader.Length);
    }

    [Fact]
    public void Constructor_ByteArray_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PeReader((byte[])null!));
    }

    [Fact]
    public void Constructor_Stream_CopiesData()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var stream = new MemoryStream(data);
        using var reader = new PeReader(stream);
        Assert.Equal(5, reader.Length);
        var span = reader.GetSpan(0, 5);
        Assert.Equal(data, span.ToArray());
    }

    [Fact]
    public void Constructor_Stream_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PeReader((Stream)null!));
    }

    [Fact]
    public void ReadByte_ReturnsCorrectValue()
    {
        using var reader = new PeReader(new byte[] { 0xAA, 0xBB, 0xCC });
        Assert.Equal(0xBB, reader.ReadByte(1));
    }

    [Fact]
    public void ReadUInt16_LittleEndian()
    {
        using var reader = new PeReader(new byte[] { 0x34, 0x12 });
        Assert.Equal((ushort)0x1234, reader.ReadUInt16(0));
    }

    [Fact]
    public void ReadUInt32_LittleEndian()
    {
        using var reader = new PeReader(new byte[] { 0x78, 0x56, 0x34, 0x12 });
        Assert.Equal(0x12345678u, reader.ReadUInt32(0));
    }

    [Fact]
    public void ReadUInt64_LittleEndian()
    {
        using var reader = new PeReader(new byte[] { 0xEF, 0xCD, 0xAB, 0x90, 0x78, 0x56, 0x34, 0x12 });
        Assert.Equal(0x1234567890ABCDEFul, reader.ReadUInt64(0));
    }

    [Fact]
    public void GetSpan_ReturnsCorrectSlice()
    {
        using var reader = new PeReader(new byte[] { 0, 1, 2, 3, 4, 5 });
        var span = reader.GetSpan(2, 3);
        Assert.Equal(new byte[] { 2, 3, 4 }, span.ToArray());
    }

    [Fact]
    public void GetSpan_BeyondEnd_ThrowsPeTruncatedException()
    {
        using var reader = new PeReader(new byte[10]);
        Assert.Throws<PeTruncatedException>(() => reader.GetSpan(5, 10));
    }

    [Fact]
    public void GetSpan_NegativeOffset_ThrowsPeTruncatedException()
    {
        using var reader = new PeReader(new byte[10]);
        Assert.Throws<PeTruncatedException>(() => reader.GetSpan(-1, 1));
    }

    [Fact]
    public void ReadFixedAscii_NullTerminated()
    {
        var data = new byte[] { (byte)'h', (byte)'i', 0, (byte)'X', (byte)'X' };
        using var reader = new PeReader(data);
        Assert.Equal("hi", reader.ReadFixedAscii(0, 5));
    }

    [Fact]
    public void ReadFixedAscii_NoNullTerminator_ReturnsFullLength()
    {
        var data = new byte[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
        using var reader = new PeReader(data);
        Assert.Equal("abcd", reader.ReadFixedAscii(0, 4));
    }

    [Fact]
    public void ReadNullTerminatedAscii_StopsAtNull()
    {
        var data = new byte[] { (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0, (byte)'X' };
        using var reader = new PeReader(data);
        Assert.Equal("test", reader.ReadNullTerminatedAscii(0));
    }

    [Fact]
    public void ReadNullTerminatedAscii_AtEnd_ReturnsEmpty()
    {
        using var reader = new PeReader(new byte[5]);
        Assert.Equal(string.Empty, reader.ReadNullTerminatedAscii(5));
    }

    [Fact]
    public void Dispose_DoubleDispose_DoesNotThrow()
    {
        var reader = new PeReader(new byte[10]);
        reader.Dispose();
        reader.Dispose(); // Should not throw
    }

    [Fact]
    public void Constructor_FilePath_ValidFile_SetsLength()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
            using var reader = new PeReader(path);
            Assert.Equal(4, reader.Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Constructor_FilePath_NonexistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => new PeReader(@"C:\nonexistent_pe_file_12345.exe"));
    }

    [Fact]
    public void Constructor_FilePath_EmptyFile_ThrowsPeTruncatedException()
    {
        var path = Path.GetTempFileName();
        try
        {
            Assert.Throws<PeTruncatedException>(() => new PeReader(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
