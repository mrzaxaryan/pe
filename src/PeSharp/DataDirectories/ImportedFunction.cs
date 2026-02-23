namespace PeSharp.DataDirectories;

public sealed class ImportedFunction
{
    public ushort Hint { get; init; }
    public string? Name { get; init; }
    public ushort? Ordinal { get; init; }
    public bool IsByOrdinal => Ordinal.HasValue;
}
