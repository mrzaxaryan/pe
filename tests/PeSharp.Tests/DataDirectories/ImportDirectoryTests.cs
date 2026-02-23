using System.Text;
using PeSharp.Tests.Helpers;

namespace PeSharp.Tests.DataDirectories;

public class ImportDirectoryTests
{
    [Fact]
    public void Parse_EmptyDirectory_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Null(file.Imports);
    }

    [Fact]
    public void Parse_ValidImportDirectory_ReturnsModules()
    {
        var pe = BuildPeWithImports(is64Bit: true);
        using var file = new PeFile(pe);

        Assert.NotNull(file.Imports);
        Assert.Single(file.Imports!.Modules);
        Assert.Equal("kernel32.dll", file.Imports.Modules[0].Name);
    }

    [Fact]
    public void Parse_ImportByName_HasNameAndHint()
    {
        var pe = BuildPeWithImports(is64Bit: true);
        using var file = new PeFile(pe);

        Assert.NotNull(file.Imports);
        var func = file.Imports!.Modules[0].Functions[0];
        Assert.Equal("ExitProcess", func.Name);
        Assert.False(func.IsByOrdinal);
    }

    [Fact]
    public void Parse_Pe32_ImportsWorkWith32BitThunks()
    {
        var pe = BuildPeWithImports(is64Bit: false);
        using var file = new PeFile(pe);

        Assert.NotNull(file.Imports);
        Assert.Single(file.Imports!.Modules);
        Assert.Equal("ExitProcess", file.Imports.Modules[0].Functions[0].Name);
    }

    [Fact]
    public void Parse_ImportByOrdinal_64Bit()
    {
        var pe = BuildPeWithOrdinalImport(is64Bit: true);
        using var file = new PeFile(pe);

        Assert.NotNull(file.Imports);
        var func = file.Imports!.Modules[0].Functions[0];
        Assert.True(func.IsByOrdinal);
        Assert.NotNull(func.Ordinal);
    }

    /// <summary>
    /// Builds a PE with an import directory importing a named function from kernel32.dll.
    /// </summary>
    private static byte[] BuildPeWithImports(bool is64Bit)
    {
        uint sectionVa = 0x1000;
        int entrySize = is64Bit ? 8 : 4;

        // Layout within import section:
        // [Import descriptor #1 (20 bytes)]
        // [Null terminator descriptor (20 bytes)]
        // [ILT entries]
        // [Hint/Name entry]
        // [Module name string]

        int iltOffset = 40; // After 2 descriptors
        int hintNameOffset = iltOffset + entrySize * 2; // ILT entry + null terminator
        int moduleNameOffset = hintNameOffset + 2 + 12; // hint (2) + "ExitProcess\0" (12)

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Import descriptor #1
        bw.Write(sectionVa + (uint)iltOffset);       // ImportLookupTableRva
        bw.Write((uint)0);                            // TimeDateStamp
        bw.Write((uint)0);                            // ForwarderChain
        bw.Write(sectionVa + (uint)moduleNameOffset); // Name RVA
        bw.Write((uint)0);                            // ImportAddressTableRva

        // Null terminator descriptor (20 zero bytes)
        for (int i = 0; i < 5; i++)
            bw.Write((uint)0);

        // ILT entry - import by name (hint/name RVA)
        if (is64Bit)
            bw.Write((ulong)(sectionVa + (uint)hintNameOffset));
        else
            bw.Write((uint)(sectionVa + (uint)hintNameOffset));

        // ILT null terminator
        if (is64Bit)
            bw.Write((ulong)0);
        else
            bw.Write((uint)0);

        // Hint/Name entry
        bw.Write((ushort)0x0001); // Hint
        bw.Write(Encoding.ASCII.GetBytes("ExitProcess"));
        bw.Write((byte)0);

        // Module name
        bw.Write(Encoding.ASCII.GetBytes("kernel32.dll"));
        bw.Write((byte)0);

        byte[] sectionData = ms.ToArray();

        var builder = new PeBuilder()
            .SetIs64Bit(is64Bit);

        var (peData, _, _) = builder.BuildWithDataDirectory(
            DataDirectoryIndex.ImportTable,
            sectionData);

        return peData;
    }

    /// <summary>
    /// Builds a PE with an import by ordinal.
    /// </summary>
    private static byte[] BuildPeWithOrdinalImport(bool is64Bit)
    {
        uint sectionVa = 0x1000;
        int entrySize = is64Bit ? 8 : 4;

        int iltOffset = 40;
        int moduleNameOffset = iltOffset + entrySize * 2;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Import descriptor
        bw.Write(sectionVa + (uint)iltOffset);
        bw.Write((uint)0);
        bw.Write((uint)0);
        bw.Write(sectionVa + (uint)moduleNameOffset);
        bw.Write((uint)0);

        // Null terminator descriptor
        for (int i = 0; i < 5; i++)
            bw.Write((uint)0);

        // ILT entry - import by ordinal
        if (is64Bit)
            bw.Write(0x8000000000000000UL | 42UL); // Ordinal flag | ordinal 42
        else
            bw.Write(0x80000000U | 42U);

        // ILT null terminator
        if (is64Bit)
            bw.Write((ulong)0);
        else
            bw.Write((uint)0);

        // Module name
        bw.Write(Encoding.ASCII.GetBytes("test.dll"));
        bw.Write((byte)0);

        byte[] sectionData = ms.ToArray();

        var builder = new PeBuilder()
            .SetIs64Bit(is64Bit);

        var (peData, _, _) = builder.BuildWithDataDirectory(
            DataDirectoryIndex.ImportTable,
            sectionData);

        return peData;
    }
}
