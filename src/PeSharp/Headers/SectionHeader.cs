namespace PeSharp.Headers;

public readonly record struct SectionHeader
{
    public required string Name { get; init; }
    public required uint VirtualSize { get; init; }
    public required uint VirtualAddress { get; init; }
    public required uint SizeOfRawData { get; init; }
    public required uint PointerToRawData { get; init; }
    public required uint PointerToRelocations { get; init; }
    public required uint PointerToLinenumbers { get; init; }
    public required ushort NumberOfRelocations { get; init; }
    public required ushort NumberOfLinenumbers { get; init; }
    public required SectionCharacteristics Characteristics { get; init; }

    public bool ContainsRva(uint rva) =>
        rva >= VirtualAddress && rva < VirtualAddress + Math.Max(VirtualSize, SizeOfRawData);

    public const int Size = 40;

    internal static SectionHeader Parse(PeReader reader, int offset)
    {
        return new SectionHeader
        {
            Name = reader.ReadFixedAscii(offset, 8),
            VirtualSize = reader.ReadUInt32(offset + 8),
            VirtualAddress = reader.ReadUInt32(offset + 12),
            SizeOfRawData = reader.ReadUInt32(offset + 16),
            PointerToRawData = reader.ReadUInt32(offset + 20),
            PointerToRelocations = reader.ReadUInt32(offset + 24),
            PointerToLinenumbers = reader.ReadUInt32(offset + 28),
            NumberOfRelocations = reader.ReadUInt16(offset + 32),
            NumberOfLinenumbers = reader.ReadUInt16(offset + 34),
            Characteristics = (SectionCharacteristics)reader.ReadUInt32(offset + 36),
        };
    }

    internal static SectionHeader[] ParseAll(PeReader reader, int offset, int count)
    {
        var sections = new SectionHeader[count];
        for (int i = 0; i < count; i++)
        {
            sections[i] = Parse(reader, offset + i * Size);
        }
        return sections;
    }
}
