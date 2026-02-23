namespace PeSharp.Headers;

public readonly record struct DosHeader
{
    public required ushort Magic { get; init; }
    public required ushort LastPageBytes { get; init; }
    public required ushort PageCount { get; init; }
    public required ushort RelocationCount { get; init; }
    public required ushort HeaderParagraphs { get; init; }
    public required ushort MinAlloc { get; init; }
    public required ushort MaxAlloc { get; init; }
    public required ushort InitialSs { get; init; }
    public required ushort InitialSp { get; init; }
    public required ushort Checksum { get; init; }
    public required ushort InitialIp { get; init; }
    public required ushort InitialCs { get; init; }
    public required ushort RelocationTableOffset { get; init; }
    public required ushort OverlayNumber { get; init; }
    public required ushort OemId { get; init; }
    public required ushort OemInfo { get; init; }
    public required int PeHeaderOffset { get; init; }

    public bool IsValid => Magic == 0x5A4D;

    public const int Size = 64;

    internal static DosHeader Parse(PeReader reader)
    {
        if (reader.Length < Size)
            throw new PeTruncatedException("File is too small for a DOS header.");

        var header = new DosHeader
        {
            Magic = reader.ReadUInt16(0x00),
            LastPageBytes = reader.ReadUInt16(0x02),
            PageCount = reader.ReadUInt16(0x04),
            RelocationCount = reader.ReadUInt16(0x06),
            HeaderParagraphs = reader.ReadUInt16(0x08),
            MinAlloc = reader.ReadUInt16(0x0A),
            MaxAlloc = reader.ReadUInt16(0x0C),
            InitialSs = reader.ReadUInt16(0x0E),
            InitialSp = reader.ReadUInt16(0x10),
            Checksum = reader.ReadUInt16(0x12),
            InitialIp = reader.ReadUInt16(0x14),
            InitialCs = reader.ReadUInt16(0x16),
            RelocationTableOffset = reader.ReadUInt16(0x18),
            OverlayNumber = reader.ReadUInt16(0x1A),
            OemId = reader.ReadUInt16(0x24),
            OemInfo = reader.ReadUInt16(0x26),
            PeHeaderOffset = (int)reader.ReadUInt32(0x3C),
        };

        if (!header.IsValid)
            throw new InvalidPeSignatureException($"Invalid DOS signature: 0x{header.Magic:X4} (expected 0x5A4D).");

        return header;
    }
}
