using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PeSharp;

internal sealed class PeReader : IDisposable
{
    private readonly MemoryMappedFile? _mmf;
    private readonly MemoryMappedViewAccessor? _accessor;
    private readonly byte[]? _data;
    private readonly int _length;
    private bool _disposed;

    public PeReader(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("PE file not found.", filePath);
        if (fileInfo.Length == 0)
            throw new PeTruncatedException("PE file is empty.");
        if (fileInfo.Length > int.MaxValue)
            throw new PeFormatException("PE file exceeds maximum supported size (2 GB).");

        _length = (int)fileInfo.Length;
        _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        _accessor = _mmf.CreateViewAccessor(0, _length, MemoryMappedFileAccess.Read);
    }

    public PeReader(byte[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _length = data.Length;
    }

    public PeReader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        _data = ms.ToArray();
        _length = _data.Length;
    }

    public int Length => _length;

    public unsafe ReadOnlySpan<byte> GetSpan(int offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > _length)
            throw new PeTruncatedException($"Cannot read {length} bytes at offset 0x{offset:X}; file is only {_length} bytes.");

        if (_data is not null)
            return _data.AsSpan(offset, length);

        byte* ptr = null;
        _accessor!.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            return new ReadOnlySpan<byte>(ptr + _accessor.PointerOffset + offset, length);
        }
        finally
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    public T Read<T>(int offset) where T : unmanaged
        => MemoryMarshal.Read<T>(GetSpan(offset, Unsafe.SizeOf<T>()));

    public byte ReadByte(int offset) => GetSpan(offset, 1)[0];
    public ushort ReadUInt16(int offset) => Read<ushort>(offset);
    public uint ReadUInt32(int offset) => Read<uint>(offset);
    public ulong ReadUInt64(int offset) => Read<ulong>(offset);

    public string ReadFixedAscii(int offset, int length)
    {
        var span = GetSpan(offset, length);
        int end = span.IndexOf((byte)0);
        if (end >= 0) span = span[..end];
        return Encoding.ASCII.GetString(span);
    }

    public string ReadNullTerminatedAscii(int offset)
    {
        int maxLen = _length - offset;
        if (maxLen <= 0) return string.Empty;

        var span = GetSpan(offset, maxLen);
        int end = span.IndexOf((byte)0);
        if (end >= 0) span = span[..end];
        return Encoding.ASCII.GetString(span);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _accessor?.Dispose();
        _mmf?.Dispose();
    }
}
