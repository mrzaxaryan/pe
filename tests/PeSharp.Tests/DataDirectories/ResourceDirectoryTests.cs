using System.Text;
using PeSharp.Tests.Helpers;

namespace PeSharp.Tests.DataDirectories;

public class ResourceDirectoryTests
{
    [Fact]
    public void Parse_EmptyDirectory_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Null(file.Resources);
    }

    [Fact]
    public void Parse_ValidResourceDirectory_ReturnsEntries()
    {
        var pe = BuildPeWithResources();
        using var file = new PeFile(pe);

        Assert.NotNull(file.Resources);
        Assert.True(file.Resources!.Entries.Count > 0);
    }

    [Fact]
    public void Parse_IdEntry_HasIdProperty()
    {
        var pe = BuildPeWithResources();
        using var file = new PeFile(pe);

        Assert.NotNull(file.Resources);
        var entry = file.Resources!.Entries[0];
        Assert.NotNull(entry.Id);
        Assert.Equal(16u, entry.Id.Value); // RT_VERSION = 16
    }

    [Fact]
    public void Parse_DataEntry_HasRvaSizeCodePage()
    {
        var pe = BuildPeWithResources();
        using var file = new PeFile(pe);

        Assert.NotNull(file.Resources);
        var entry = file.Resources!.Entries[0];
        Assert.NotNull(entry.Data);
        Assert.True(entry.Data!.Size > 0);
    }

    [Fact]
    public void Parse_DirectoryMetadata_IsPopulated()
    {
        var pe = BuildPeWithResources();
        using var file = new PeFile(pe);

        Assert.NotNull(file.Resources);
        // The resource directory should have parsed without errors
        Assert.Equal(0u, file.Resources!.Characteristics);
    }

    private static byte[] BuildPeWithResources()
    {
        // Simple resource directory with one ID entry pointing to a data entry
        // Layout:
        // [Directory Table (16 bytes)]
        // [Directory Entry (8 bytes)] - ID entry pointing to data entry
        // [Data Entry (16 bytes)]

        int dataEntryOffset = 24;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Resource Directory Table
        bw.Write((uint)0);           // Characteristics
        bw.Write((uint)0);           // TimeDateStamp
        bw.Write((ushort)0);         // MajorVersion
        bw.Write((ushort)0);         // MinorVersion
        bw.Write((ushort)0);         // NumberOfNamedEntries
        bw.Write((ushort)1);         // NumberOfIdEntries

        // Resource Directory Entry (ID entry -> data)
        bw.Write((uint)16);                   // Id = RT_VERSION (16)
        bw.Write((uint)dataEntryOffset);       // Offset to data entry (no high bit = data entry)

        // Resource Data Entry
        bw.Write((uint)0x3000);    // DataRva (arbitrary)
        bw.Write((uint)0x100);     // Size
        bw.Write((uint)0);         // CodePage
        bw.Write((uint)0);         // Reserved

        byte[] resourceData = ms.ToArray();

        var builder = new PeBuilder()
            .SetIs64Bit(true);

        var (peData, _, _) = builder.BuildWithDataDirectory(
            DataDirectoryIndex.ResourceTable,
            resourceData);

        return peData;
    }
}
