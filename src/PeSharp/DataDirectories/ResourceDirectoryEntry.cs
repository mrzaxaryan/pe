namespace PeSharp.DataDirectories;

public sealed class ResourceDirectoryEntry
{
    public uint? Id { get; init; }
    public string? Name { get; init; }
    public ResourceDirectory? Subdirectory { get; init; }
    public ResourceDataEntry? Data { get; init; }
    public bool IsDirectory => Subdirectory is not null;
    public bool IsNamed => Name is not null;
}
