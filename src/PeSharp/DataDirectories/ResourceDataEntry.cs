namespace PeSharp.DataDirectories;

public sealed class ResourceDataEntry
{
    public required uint Rva { get; init; }
    public required uint Size { get; init; }
    public required uint CodePage { get; init; }
}
