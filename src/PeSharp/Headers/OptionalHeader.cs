namespace PeSharp.Headers;

public readonly record struct OptionalHeader
{
    // Standard fields
    public required PeMagic Magic { get; init; }
    public required byte MajorLinkerVersion { get; init; }
    public required byte MinorLinkerVersion { get; init; }
    public required uint SizeOfCode { get; init; }
    public required uint SizeOfInitializedData { get; init; }
    public required uint SizeOfUninitializedData { get; init; }
    public required uint AddressOfEntryPoint { get; init; }
    public required uint BaseOfCode { get; init; }
    public required uint BaseOfData { get; init; } // PE32 only; 0 for PE32+

    // Windows-specific fields
    public required ulong ImageBase { get; init; }
    public required uint SectionAlignment { get; init; }
    public required uint FileAlignment { get; init; }
    public required ushort MajorOperatingSystemVersion { get; init; }
    public required ushort MinorOperatingSystemVersion { get; init; }
    public required ushort MajorImageVersion { get; init; }
    public required ushort MinorImageVersion { get; init; }
    public required ushort MajorSubsystemVersion { get; init; }
    public required ushort MinorSubsystemVersion { get; init; }
    public required uint Win32VersionValue { get; init; }
    public required uint SizeOfImage { get; init; }
    public required uint SizeOfHeaders { get; init; }
    public required uint CheckSum { get; init; }
    public required Subsystem Subsystem { get; init; }
    public required DllCharacteristics DllCharacteristics { get; init; }
    public required ulong SizeOfStackReserve { get; init; }
    public required ulong SizeOfStackCommit { get; init; }
    public required ulong SizeOfHeapReserve { get; init; }
    public required ulong SizeOfHeapCommit { get; init; }
    public required uint LoaderFlags { get; init; }
    public required uint NumberOfRvaAndSizes { get; init; }

    public required IReadOnlyList<DataDirectory> DataDirectories { get; init; }

    public bool IsPe32 => Magic == PeMagic.Pe32;
    public bool IsPe32Plus => Magic == PeMagic.Pe32Plus;

    internal static OptionalHeader Parse(PeReader reader, int offset)
    {
        var magic = (PeMagic)reader.ReadUInt16(offset);
        bool is64 = magic == PeMagic.Pe32Plus;

        if (magic != PeMagic.Pe32 && magic != PeMagic.Pe32Plus)
            throw new PeFormatException($"Unknown Optional Header magic: 0x{(ushort)magic:X4}.");

        int pos = offset + 2;

        byte majorLinkerVersion = reader.ReadByte(pos); pos += 1;
        byte minorLinkerVersion = reader.ReadByte(pos); pos += 1;
        uint sizeOfCode = reader.ReadUInt32(pos); pos += 4;
        uint sizeOfInitializedData = reader.ReadUInt32(pos); pos += 4;
        uint sizeOfUninitializedData = reader.ReadUInt32(pos); pos += 4;
        uint addressOfEntryPoint = reader.ReadUInt32(pos); pos += 4;
        uint baseOfCode = reader.ReadUInt32(pos); pos += 4;

        uint baseOfData = 0;
        if (!is64)
        {
            baseOfData = reader.ReadUInt32(pos); pos += 4;
        }

        ulong imageBase;
        if (is64) { imageBase = reader.ReadUInt64(pos); pos += 8; }
        else { imageBase = reader.ReadUInt32(pos); pos += 4; }

        uint sectionAlignment = reader.ReadUInt32(pos); pos += 4;
        uint fileAlignment = reader.ReadUInt32(pos); pos += 4;
        ushort majorOsVersion = reader.ReadUInt16(pos); pos += 2;
        ushort minorOsVersion = reader.ReadUInt16(pos); pos += 2;
        ushort majorImageVersion = reader.ReadUInt16(pos); pos += 2;
        ushort minorImageVersion = reader.ReadUInt16(pos); pos += 2;
        ushort majorSubsystemVersion = reader.ReadUInt16(pos); pos += 2;
        ushort minorSubsystemVersion = reader.ReadUInt16(pos); pos += 2;
        uint win32VersionValue = reader.ReadUInt32(pos); pos += 4;
        uint sizeOfImage = reader.ReadUInt32(pos); pos += 4;
        uint sizeOfHeaders = reader.ReadUInt32(pos); pos += 4;
        uint checkSum = reader.ReadUInt32(pos); pos += 4;
        var subsystem = (Subsystem)reader.ReadUInt16(pos); pos += 2;
        var dllCharacteristics = (DllCharacteristics)reader.ReadUInt16(pos); pos += 2;

        ulong sizeOfStackReserve, sizeOfStackCommit, sizeOfHeapReserve, sizeOfHeapCommit;
        if (is64)
        {
            sizeOfStackReserve = reader.ReadUInt64(pos); pos += 8;
            sizeOfStackCommit = reader.ReadUInt64(pos); pos += 8;
            sizeOfHeapReserve = reader.ReadUInt64(pos); pos += 8;
            sizeOfHeapCommit = reader.ReadUInt64(pos); pos += 8;
        }
        else
        {
            sizeOfStackReserve = reader.ReadUInt32(pos); pos += 4;
            sizeOfStackCommit = reader.ReadUInt32(pos); pos += 4;
            sizeOfHeapReserve = reader.ReadUInt32(pos); pos += 4;
            sizeOfHeapCommit = reader.ReadUInt32(pos); pos += 4;
        }

        uint loaderFlags = reader.ReadUInt32(pos); pos += 4;
        uint numberOfRvaAndSizes = reader.ReadUInt32(pos); pos += 4;

        int dirCount = (int)Math.Min(numberOfRvaAndSizes, 16);
        var dataDirectories = new DataDirectory[dirCount];
        for (int i = 0; i < dirCount; i++)
        {
            uint va = reader.ReadUInt32(pos); pos += 4;
            uint size = reader.ReadUInt32(pos); pos += 4;
            dataDirectories[i] = new DataDirectory(va, size);
        }

        return new OptionalHeader
        {
            Magic = magic,
            MajorLinkerVersion = majorLinkerVersion,
            MinorLinkerVersion = minorLinkerVersion,
            SizeOfCode = sizeOfCode,
            SizeOfInitializedData = sizeOfInitializedData,
            SizeOfUninitializedData = sizeOfUninitializedData,
            AddressOfEntryPoint = addressOfEntryPoint,
            BaseOfCode = baseOfCode,
            BaseOfData = baseOfData,
            ImageBase = imageBase,
            SectionAlignment = sectionAlignment,
            FileAlignment = fileAlignment,
            MajorOperatingSystemVersion = majorOsVersion,
            MinorOperatingSystemVersion = minorOsVersion,
            MajorImageVersion = majorImageVersion,
            MinorImageVersion = minorImageVersion,
            MajorSubsystemVersion = majorSubsystemVersion,
            MinorSubsystemVersion = minorSubsystemVersion,
            Win32VersionValue = win32VersionValue,
            SizeOfImage = sizeOfImage,
            SizeOfHeaders = sizeOfHeaders,
            CheckSum = checkSum,
            Subsystem = subsystem,
            DllCharacteristics = dllCharacteristics,
            SizeOfStackReserve = sizeOfStackReserve,
            SizeOfStackCommit = sizeOfStackCommit,
            SizeOfHeapReserve = sizeOfHeapReserve,
            SizeOfHeapCommit = sizeOfHeapCommit,
            LoaderFlags = loaderFlags,
            NumberOfRvaAndSizes = numberOfRvaAndSizes,
            DataDirectories = dataDirectories,
        };
    }
}
