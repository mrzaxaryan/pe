namespace PeSharp.DataDirectories;

public sealed class ImportDirectory
{
    public required IReadOnlyList<ImportedModule> Modules { get; init; }

    internal static ImportDirectory? Parse(PeReader reader, RvaResolver resolver, Headers.DataDirectory dir, bool is64Bit)
    {
        if (dir.IsEmpty) return null;

        var fileOffset = resolver.ResolveRva(dir.VirtualAddress);
        if (fileOffset is null) return null;

        var modules = new List<ImportedModule>();
        int offset = (int)fileOffset.Value;

        while (true)
        {
            // Each import descriptor is 20 bytes
            uint importLookupTableRva = reader.ReadUInt32(offset);
            uint timeDateStamp = reader.ReadUInt32(offset + 4);
            uint forwarderChain = reader.ReadUInt32(offset + 8);
            uint nameRva = reader.ReadUInt32(offset + 12);
            uint importAddressTableRva = reader.ReadUInt32(offset + 16);

            // Null terminator: all fields zero
            if (importLookupTableRva == 0 && nameRva == 0)
                break;

            var nameFileOffset = resolver.ResolveRva(nameRva);
            string moduleName = nameFileOffset is not null
                ? reader.ReadNullTerminatedAscii((int)nameFileOffset.Value)
                : string.Empty;

            // Use ILT if available, otherwise fall back to IAT
            uint thunkRva = importLookupTableRva != 0 ? importLookupTableRva : importAddressTableRva;
            var functions = ParseThunks(reader, resolver, thunkRva, is64Bit);

            modules.Add(new ImportedModule
            {
                Name = moduleName,
                TimeDateStamp = timeDateStamp,
                ForwarderChain = forwarderChain,
                Functions = functions,
            });

            offset += 20;
        }

        return new ImportDirectory { Modules = modules };
    }

    private static List<ImportedFunction> ParseThunks(PeReader reader, RvaResolver resolver, uint thunkRva, bool is64Bit)
    {
        var functions = new List<ImportedFunction>();
        var thunkFileOffset = resolver.ResolveRva(thunkRva);
        if (thunkFileOffset is null) return functions;

        int pos = (int)thunkFileOffset.Value;
        int entrySize = is64Bit ? 8 : 4;
        ulong ordinalFlag = is64Bit ? 0x8000000000000000UL : 0x80000000UL;

        while (true)
        {
            ulong entry;
            if (is64Bit)
                entry = reader.ReadUInt64(pos);
            else
                entry = reader.ReadUInt32(pos);

            if (entry == 0) break;

            if ((entry & ordinalFlag) != 0)
            {
                // Import by ordinal
                functions.Add(new ImportedFunction
                {
                    Ordinal = (ushort)(entry & 0xFFFF),
                });
            }
            else
            {
                // Import by name: entry is an RVA to hint/name table entry
                uint hintNameRva = (uint)(entry & 0x7FFFFFFF);
                var hintNameFileOffset = resolver.ResolveRva(hintNameRva);
                if (hintNameFileOffset is not null)
                {
                    ushort hint = reader.ReadUInt16((int)hintNameFileOffset.Value);
                    string name = reader.ReadNullTerminatedAscii((int)hintNameFileOffset.Value + 2);
                    functions.Add(new ImportedFunction
                    {
                        Hint = hint,
                        Name = name,
                    });
                }
            }

            pos += entrySize;
        }

        return functions;
    }
}
