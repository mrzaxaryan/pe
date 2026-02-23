namespace PeSharp.DataDirectories;

public sealed class BaseRelocationBlock
{
    public required uint PageRva { get; init; }
    public required IReadOnlyList<BaseRelocationEntry> Entries { get; init; }
}
