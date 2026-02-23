namespace PeSharp.DataDirectories;

public sealed class ExportDirectory
{
    public required uint Characteristics { get; init; }
    public required uint TimeDateStamp { get; init; }
    public required ushort MajorVersion { get; init; }
    public required ushort MinorVersion { get; init; }
    public required string Name { get; init; }
    public required uint OrdinalBase { get; init; }
    public required IReadOnlyList<ExportedFunction> Functions { get; init; }

    internal static ExportDirectory? Parse(PeReader reader, RvaResolver resolver, Headers.DataDirectory dir)
    {
        if (dir.IsEmpty) return null;

        var fileOffset = resolver.ResolveRva(dir.VirtualAddress);
        if (fileOffset is null) return null;

        int offset = (int)fileOffset.Value;

        uint characteristics = reader.ReadUInt32(offset);
        uint timeDateStamp = reader.ReadUInt32(offset + 4);
        ushort majorVersion = reader.ReadUInt16(offset + 8);
        ushort minorVersion = reader.ReadUInt16(offset + 10);
        uint nameRva = reader.ReadUInt32(offset + 12);
        uint ordinalBase = reader.ReadUInt32(offset + 16);
        uint numberOfFunctions = reader.ReadUInt32(offset + 20);
        uint numberOfNames = reader.ReadUInt32(offset + 24);
        uint addressOfFunctions = reader.ReadUInt32(offset + 28);
        uint addressOfNames = reader.ReadUInt32(offset + 32);
        uint addressOfNameOrdinals = reader.ReadUInt32(offset + 36);

        var nameFileOffset = resolver.ResolveRva(nameRva);
        string dllName = nameFileOffset is not null
            ? reader.ReadNullTerminatedAscii((int)nameFileOffset.Value)
            : string.Empty;

        var functionsFileOffset = resolver.ResolveRva(addressOfFunctions);
        var namesFileOffset = resolver.ResolveRva(addressOfNames);
        var ordinalsFileOffset = resolver.ResolveRva(addressOfNameOrdinals);

        if (functionsFileOffset is null) return null;

        // Build ordinal -> name mapping
        var ordinalToName = new Dictionary<uint, string>();
        if (namesFileOffset is not null && ordinalsFileOffset is not null)
        {
            for (uint i = 0; i < numberOfNames; i++)
            {
                uint nameEntryRva = reader.ReadUInt32((int)namesFileOffset.Value + (int)(i * 4));
                ushort ordinalIndex = reader.ReadUInt16((int)ordinalsFileOffset.Value + (int)(i * 2));

                var nameEntryFileOffset = resolver.ResolveRva(nameEntryRva);
                if (nameEntryFileOffset is not null)
                {
                    string funcName = reader.ReadNullTerminatedAscii((int)nameEntryFileOffset.Value);
                    ordinalToName[ordinalIndex] = funcName;
                }
            }
        }

        // Export directory RVA range for detecting forwarders
        uint exportDirStart = dir.VirtualAddress;
        uint exportDirEnd = dir.VirtualAddress + dir.Size;

        var functions = new List<ExportedFunction>((int)numberOfFunctions);
        for (uint i = 0; i < numberOfFunctions; i++)
        {
            uint funcRva = reader.ReadUInt32((int)functionsFileOffset.Value + (int)(i * 4));
            if (funcRva == 0) continue;

            string? forwarderName = null;
            if (funcRva >= exportDirStart && funcRva < exportDirEnd)
            {
                var forwarderOffset = resolver.ResolveRva(funcRva);
                if (forwarderOffset is not null)
                    forwarderName = reader.ReadNullTerminatedAscii((int)forwarderOffset.Value);
            }

            ordinalToName.TryGetValue(i, out string? name);

            functions.Add(new ExportedFunction
            {
                Ordinal = i + ordinalBase,
                Address = funcRva,
                Name = name,
                ForwarderName = forwarderName,
            });
        }

        return new ExportDirectory
        {
            Characteristics = characteristics,
            TimeDateStamp = timeDateStamp,
            MajorVersion = majorVersion,
            MinorVersion = minorVersion,
            Name = dllName,
            OrdinalBase = ordinalBase,
            Functions = functions,
        };
    }
}
