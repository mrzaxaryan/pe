using PeSharp.Tests.Helpers;

namespace PeSharp.Tests.DataDirectories;

public class DebugDirectoryTests
{
    [Fact]
    public void Parse_EmptyDirectory_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Null(file.DebugInfo);
    }

    [Fact]
    public void Parse_ValidDebugDirectory_ReturnsEntries()
    {
        var pe = BuildPeWithDebugDirectory(entryCount: 1);
        using var file = new PeFile(pe);

        Assert.NotNull(file.DebugInfo);
        Assert.Single(file.DebugInfo!.Entries);
    }

    [Fact]
    public void Parse_Entry_AllFieldsPopulated()
    {
        var pe = BuildPeWithDebugDirectory(entryCount: 1);
        using var file = new PeFile(pe);

        Assert.NotNull(file.DebugInfo);
        var entry = file.DebugInfo!.Entries[0];
        Assert.Equal(DebugType.CodeView, entry.Type);
        Assert.Equal(0x60000000u, entry.TimeDateStamp);
        Assert.Equal((ushort)1, entry.MajorVersion);
        Assert.Equal((ushort)0, entry.MinorVersion);
        Assert.Equal(0x100u, entry.SizeOfData);
    }

    [Fact]
    public void Parse_MultipleEntries_CountMatchesDirSize()
    {
        var pe = BuildPeWithDebugDirectory(entryCount: 3);
        using var file = new PeFile(pe);

        Assert.NotNull(file.DebugInfo);
        Assert.Equal(3, file.DebugInfo!.Entries.Count);
    }

    private static byte[] BuildPeWithDebugDirectory(int entryCount)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        for (int i = 0; i < entryCount; i++)
        {
            bw.Write((uint)0);              // Characteristics
            bw.Write((uint)0x60000000);     // TimeDateStamp
            bw.Write((ushort)1);            // MajorVersion
            bw.Write((ushort)0);            // MinorVersion
            bw.Write((uint)2);              // Type = CodeView
            bw.Write((uint)0x100);          // SizeOfData
            bw.Write((uint)0x3000);         // AddressOfRawData
            bw.Write((uint)0x1000);         // PointerToRawData
        }

        byte[] debugData = ms.ToArray();

        var builder = new PeBuilder()
            .SetIs64Bit(true);

        var (peData, _, _) = builder.BuildWithDataDirectory(
            DataDirectoryIndex.Debug,
            debugData);

        return peData;
    }
}
