using PeSharp.Tests.Helpers;

namespace PeSharp.Tests.DataDirectories;

public class BaseRelocationTests
{
    [Fact]
    public void Parse_EmptyDirectory_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Null(file.Relocations);
    }

    [Fact]
    public void Parse_ValidRelocation_ReturnsBlocks()
    {
        var pe = BuildPeWithRelocations();
        using var file = new PeFile(pe);

        Assert.NotNull(file.Relocations);
        Assert.Single(file.Relocations!.Blocks);
    }

    [Fact]
    public void Parse_Block_HasPageRvaAndEntries()
    {
        var pe = BuildPeWithRelocations();
        using var file = new PeFile(pe);

        Assert.NotNull(file.Relocations);
        var block = file.Relocations!.Blocks[0];
        Assert.Equal(0x1000u, block.PageRva);
        Assert.True(block.Entries.Count > 0);
    }

    [Fact]
    public void Parse_Entry_TypeAndOffset_DecodedCorrectly()
    {
        var pe = BuildPeWithRelocations();
        using var file = new PeFile(pe);

        Assert.NotNull(file.Relocations);
        var entry = file.Relocations!.Blocks[0].Entries[0];
        Assert.Equal(BaseRelocationType.Dir64, entry.Type);
        Assert.Equal((ushort)0x100, entry.Offset);
    }

    [Fact]
    public void Parse_AbsoluteEntries_AreFilteredOut()
    {
        var pe = BuildPeWithRelocations(includeAbsolute: true);
        using var file = new PeFile(pe);

        Assert.NotNull(file.Relocations);
        // Absolute entries (type 0) should be filtered out
        foreach (var entry in file.Relocations!.Blocks[0].Entries)
        {
            Assert.NotEqual(BaseRelocationType.Absolute, entry.Type);
        }
    }

    private static byte[] BuildPeWithRelocations(bool includeAbsolute = false)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Relocation block
        int entryCount = includeAbsolute ? 3 : 2;
        uint blockSize = (uint)(8 + entryCount * 2);

        bw.Write((uint)0x1000);     // PageRva
        bw.Write(blockSize);         // BlockSize

        // Entry 1: Dir64 (type 10) at offset 0x100
        bw.Write((ushort)((10 << 12) | 0x100));

        // Entry 2: HighLow (type 3) at offset 0x200
        bw.Write((ushort)((3 << 12) | 0x200));

        if (includeAbsolute)
        {
            // Entry 3: Absolute (type 0) - padding, should be filtered
            bw.Write((ushort)0x0000);
        }

        byte[] relocData = ms.ToArray();

        var builder = new PeBuilder()
            .SetIs64Bit(true);

        var (peData, _, _) = builder.BuildWithDataDirectory(
            DataDirectoryIndex.BaseRelocationTable,
            relocData);

        return peData;
    }
}
