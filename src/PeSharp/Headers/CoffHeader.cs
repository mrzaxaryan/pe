namespace PeSharp.Headers;

public readonly record struct CoffHeader
{
    public required MachineType Machine { get; init; }
    public required ushort NumberOfSections { get; init; }
    public required uint TimeDateStamp { get; init; }
    public required uint PointerToSymbolTable { get; init; }
    public required uint NumberOfSymbols { get; init; }
    public required ushort SizeOfOptionalHeader { get; init; }
    public required FileCharacteristics Characteristics { get; init; }

    public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeSeconds(TimeDateStamp);
    public bool IsDll => Characteristics.HasFlag(FileCharacteristics.Dll);
    public bool IsExecutable => Characteristics.HasFlag(FileCharacteristics.ExecutableImage);

    public const int Size = 20;

    internal static CoffHeader Parse(PeReader reader, int offset)
    {
        return new CoffHeader
        {
            Machine = (MachineType)reader.ReadUInt16(offset),
            NumberOfSections = reader.ReadUInt16(offset + 2),
            TimeDateStamp = reader.ReadUInt32(offset + 4),
            PointerToSymbolTable = reader.ReadUInt32(offset + 8),
            NumberOfSymbols = reader.ReadUInt32(offset + 12),
            SizeOfOptionalHeader = reader.ReadUInt16(offset + 16),
            Characteristics = (FileCharacteristics)reader.ReadUInt16(offset + 18),
        };
    }
}
