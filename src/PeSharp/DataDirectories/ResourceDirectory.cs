using System.Text;

namespace PeSharp.DataDirectories;

public sealed class ResourceDirectory
{
    public required uint Characteristics { get; init; }
    public required uint TimeDateStamp { get; init; }
    public required ushort MajorVersion { get; init; }
    public required ushort MinorVersion { get; init; }
    public required IReadOnlyList<ResourceDirectoryEntry> Entries { get; init; }

    internal static ResourceDirectory? Parse(PeReader reader, RvaResolver resolver, Headers.DataDirectory dir)
    {
        if (dir.IsEmpty) return null;

        var fileOffset = resolver.ResolveRva(dir.VirtualAddress);
        if (fileOffset is null) return null;

        int resourceBase = (int)fileOffset.Value;
        return ParseDirectory(reader, resolver, resourceBase, resourceBase, 0);
    }

    private static ResourceDirectory ParseDirectory(PeReader reader, RvaResolver resolver, int resourceBase, int offset, int depth)
    {
        if (depth > 10)
            throw new PeFormatException("Resource directory recursion depth exceeded.");

        uint characteristics = reader.ReadUInt32(offset);
        uint timeDateStamp = reader.ReadUInt32(offset + 4);
        ushort majorVersion = reader.ReadUInt16(offset + 8);
        ushort minorVersion = reader.ReadUInt16(offset + 10);
        ushort namedEntryCount = reader.ReadUInt16(offset + 12);
        ushort idEntryCount = reader.ReadUInt16(offset + 14);

        int totalEntries = namedEntryCount + idEntryCount;
        var entries = new List<ResourceDirectoryEntry>(totalEntries);
        int entryOffset = offset + 16;

        for (int i = 0; i < totalEntries; i++)
        {
            uint nameOrId = reader.ReadUInt32(entryOffset);
            uint dataOrSubdir = reader.ReadUInt32(entryOffset + 4);

            string? name = null;
            uint? id = null;

            if ((nameOrId & 0x80000000) != 0)
            {
                // Named entry: offset to unicode string
                int nameOffset = resourceBase + (int)(nameOrId & 0x7FFFFFFF);
                ushort nameLength = reader.ReadUInt16(nameOffset);
                var nameBytes = reader.GetSpan(nameOffset + 2, nameLength * 2);
                name = Encoding.Unicode.GetString(nameBytes);
            }
            else
            {
                id = nameOrId;
            }

            if ((dataOrSubdir & 0x80000000) != 0)
            {
                // Subdirectory
                int subdirOffset = resourceBase + (int)(dataOrSubdir & 0x7FFFFFFF);
                var subdir = ParseDirectory(reader, resolver, resourceBase, subdirOffset, depth + 1);
                entries.Add(new ResourceDirectoryEntry
                {
                    Id = id,
                    Name = name,
                    Subdirectory = subdir,
                });
            }
            else
            {
                // Data entry
                int dataEntryOffset = resourceBase + (int)dataOrSubdir;
                uint dataRva = reader.ReadUInt32(dataEntryOffset);
                uint dataSize = reader.ReadUInt32(dataEntryOffset + 4);
                uint codePage = reader.ReadUInt32(dataEntryOffset + 8);

                entries.Add(new ResourceDirectoryEntry
                {
                    Id = id,
                    Name = name,
                    Data = new ResourceDataEntry
                    {
                        Rva = dataRva,
                        Size = dataSize,
                        CodePage = codePage,
                    },
                });
            }

            entryOffset += 8;
        }

        return new ResourceDirectory
        {
            Characteristics = characteristics,
            TimeDateStamp = timeDateStamp,
            MajorVersion = majorVersion,
            MinorVersion = minorVersion,
            Entries = entries,
        };
    }
}
