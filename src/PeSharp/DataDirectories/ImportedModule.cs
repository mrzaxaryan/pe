namespace PeSharp.DataDirectories;

public sealed class ImportedModule
{
    public required string Name { get; init; }
    public required uint TimeDateStamp { get; init; }
    public required uint ForwarderChain { get; init; }
    public required IReadOnlyList<ImportedFunction> Functions { get; init; }
}
