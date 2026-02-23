namespace PeSharp.DataDirectories;

public sealed class BaseRelocation
{
    public required IReadOnlyList<BaseRelocationBlock> Blocks { get; init; }

    internal static BaseRelocation? Parse(PeReader reader, RvaResolver resolver, Headers.DataDirectory dir)
    {
        if (dir.IsEmpty) return null;

        var fileOffset = resolver.ResolveRva(dir.VirtualAddress);
        if (fileOffset is null) return null;

        var blocks = new List<BaseRelocationBlock>();
        int pos = (int)fileOffset.Value;
        int end = pos + (int)dir.Size;

        while (pos < end)
        {
            uint pageRva = reader.ReadUInt32(pos);
            uint blockSize = reader.ReadUInt32(pos + 4);

            if (blockSize == 0) break;

            int entryCount = ((int)blockSize - 8) / 2;
            var entries = new List<BaseRelocationEntry>(entryCount);

            for (int i = 0; i < entryCount; i++)
            {
                ushort value = reader.ReadUInt16(pos + 8 + i * 2);
                var type = (BaseRelocationType)(value >> 12);
                ushort offset = (ushort)(value & 0x0FFF);

                if (type != BaseRelocationType.Absolute)
                    entries.Add(new BaseRelocationEntry(type, offset));
            }

            blocks.Add(new BaseRelocationBlock
            {
                PageRva = pageRva,
                Entries = entries,
            });

            pos += (int)blockSize;
        }

        return new BaseRelocation { Blocks = blocks };
    }
}
