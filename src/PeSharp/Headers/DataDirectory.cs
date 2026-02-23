namespace PeSharp.Headers;

public readonly record struct DataDirectory(uint VirtualAddress, uint Size)
{
    public bool IsEmpty => VirtualAddress == 0 && Size == 0;
    public const int StructSize = 8;
}
