namespace PeSharp.DataDirectories;

public sealed class ExportedFunction
{
    public required uint Ordinal { get; init; }
    public required uint Address { get; init; }
    public string? Name { get; init; }
    public string? ForwarderName { get; init; }
    public bool IsForwarder => ForwarderName is not null;
}
