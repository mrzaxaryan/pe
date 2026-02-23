namespace PeSharp.DataDirectories;

public sealed class DebugDirectory
{
    public required IReadOnlyList<DebugDirectoryEntry> Entries { get; init; }

    internal static DebugDirectory? Parse(PeReader reader, RvaResolver resolver, Headers.DataDirectory dir)
    {
        if (dir.IsEmpty) return null;

        var fileOffset = resolver.ResolveRva(dir.VirtualAddress);
        if (fileOffset is null) return null;

        int offset = (int)fileOffset.Value;
        int entrySize = 28; // sizeof(IMAGE_DEBUG_DIRECTORY)
        int count = (int)dir.Size / entrySize;

        var entries = new List<DebugDirectoryEntry>(count);
        for (int i = 0; i < count; i++)
        {
            int pos = offset + i * entrySize;
            entries.Add(new DebugDirectoryEntry
            {
                Characteristics = reader.ReadUInt32(pos),
                TimeDateStamp = reader.ReadUInt32(pos + 4),
                MajorVersion = reader.ReadUInt16(pos + 8),
                MinorVersion = reader.ReadUInt16(pos + 10),
                Type = (DebugType)reader.ReadUInt32(pos + 12),
                SizeOfData = reader.ReadUInt32(pos + 16),
                AddressOfRawData = reader.ReadUInt32(pos + 20),
                PointerToRawData = reader.ReadUInt32(pos + 24),
            });
        }

        return new DebugDirectory { Entries = entries };
    }
}
