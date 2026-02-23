namespace PeSharp.DataDirectories;

public sealed class DebugDirectoryEntry
{
    public required uint Characteristics { get; init; }
    public required uint TimeDateStamp { get; init; }
    public required ushort MajorVersion { get; init; }
    public required ushort MinorVersion { get; init; }
    public required DebugType Type { get; init; }
    public required uint SizeOfData { get; init; }
    public required uint AddressOfRawData { get; init; }
    public required uint PointerToRawData { get; init; }
}
