using System.Text;
using PeSharp.Tests.Helpers;

namespace PeSharp.Tests.DataDirectories;

public class ExportDirectoryTests
{
    [Fact]
    public void Parse_EmptyDirectory_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Null(file.Exports);
    }

    [Fact]
    public void Parse_ValidExportDirectory_ReturnsMetadata()
    {
        var (pe, exports) = BuildPeWithExports();
        using var file = new PeFile(pe);

        Assert.NotNull(file.Exports);
        Assert.Equal("test.dll", file.Exports!.Name);
        Assert.Equal(1u, file.Exports.OrdinalBase);
    }

    [Fact]
    public void Parse_NamedFunction_HasNameAndOrdinal()
    {
        var (pe, _) = BuildPeWithExports();
        using var file = new PeFile(pe);

        Assert.NotNull(file.Exports);
        Assert.True(file.Exports!.Functions.Count > 0);

        var func = file.Exports.Functions[0];
        Assert.Equal("TestFunc", func.Name);
        Assert.Equal(1u, func.Ordinal);
        Assert.False(func.IsForwarder);
    }

    [Fact]
    public void Parse_MultipleFunctions_AllParsed()
    {
        var (pe, _) = BuildPeWithExports(functionCount: 3);
        using var file = new PeFile(pe);

        Assert.NotNull(file.Exports);
        Assert.Equal(3, file.Exports!.Functions.Count);
    }

    [Fact]
    public void Parse_FunctionWithZeroRva_IsSkipped()
    {
        var (pe, _) = BuildPeWithExports(includeZeroRvaFunction: true);
        using var file = new PeFile(pe);

        Assert.NotNull(file.Exports);
        // Zero-RVA functions are skipped, so count should not include them
        foreach (var func in file.Exports!.Functions)
        {
            Assert.NotEqual(0u, func.Address);
        }
    }

    /// <summary>
    /// Builds a PE with a valid export directory containing named functions.
    /// </summary>
    private static (byte[] pe, byte[] exportData) BuildPeWithExports(int functionCount = 1, bool includeZeroRvaFunction = false)
    {
        // Layout of export section data:
        // [Export Directory Table (40 bytes)]
        // [Function Address Table (4 bytes per function)]
        // [Name Pointer Table (4 bytes per name)]
        // [Ordinal Table (2 bytes per name)]
        // [DLL Name string]
        // [Function name strings]

        int actualFuncCount = functionCount + (includeZeroRvaFunction ? 1 : 0);
        int nameCount = functionCount; // Named functions only

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        int funcTableOffset = 40;
        int nameTableOffset = funcTableOffset + actualFuncCount * 4;
        int ordinalTableOffset = nameTableOffset + nameCount * 4;
        int stringsOffset = ordinalTableOffset + nameCount * 2;

        // We'll compute string RVAs later, so first build strings
        string dllName = "test.dll";
        var funcNames = new List<string>();
        for (int i = 0; i < functionCount; i++)
            funcNames.Add(i == 0 ? "TestFunc" : $"Func{i}");

        // Calculate string positions
        int dllNameOffset = stringsOffset;
        int nextStringOffset = dllNameOffset + dllName.Length + 1;
        var funcNameOffsets = new List<int>();
        foreach (var name in funcNames)
        {
            funcNameOffsets.Add(nextStringOffset);
            nextStringOffset += name.Length + 1;
        }

        int totalSize = nextStringOffset;

        // Section VA will be 0x1000 (first section)
        uint sectionVa = 0x1000;

        // Write Export Directory Table
        bw.Write((uint)0);                              // Characteristics
        bw.Write((uint)0x60000000);                      // TimeDateStamp
        bw.Write((ushort)0);                             // MajorVersion
        bw.Write((ushort)0);                             // MinorVersion
        bw.Write(sectionVa + (uint)dllNameOffset);       // Name RVA
        bw.Write((uint)1);                               // OrdinalBase
        bw.Write((uint)actualFuncCount);                 // NumberOfFunctions
        bw.Write((uint)nameCount);                       // NumberOfNames
        bw.Write(sectionVa + (uint)funcTableOffset);     // AddressOfFunctions
        bw.Write(sectionVa + (uint)nameTableOffset);     // AddressOfNames
        bw.Write(sectionVa + (uint)ordinalTableOffset);  // AddressOfNameOrdinals

        // Function Address Table
        if (includeZeroRvaFunction)
            bw.Write((uint)0); // Zero RVA - should be skipped

        for (int i = 0; i < functionCount; i++)
            bw.Write(sectionVa + (uint)0x500 + (uint)i); // Arbitrary function RVAs outside export dir

        // Name Pointer Table
        for (int i = 0; i < nameCount; i++)
            bw.Write(sectionVa + (uint)funcNameOffsets[i]);

        // Ordinal Table (indices into function table)
        for (int i = 0; i < nameCount; i++)
            bw.Write((ushort)(includeZeroRvaFunction ? i + 1 : i));

        // DLL Name
        bw.Write(Encoding.ASCII.GetBytes(dllName));
        bw.Write((byte)0);

        // Function Names
        foreach (var name in funcNames)
        {
            bw.Write(Encoding.ASCII.GetBytes(name));
            bw.Write((byte)0);
        }

        byte[] exportSectionData = ms.ToArray();

        // Need enough section data for the "function addresses" we pointed to
        int sectionSize = Math.Max(exportSectionData.Length, 0x600);
        byte[] sectionData = new byte[sectionSize];
        Array.Copy(exportSectionData, sectionData, exportSectionData.Length);

        // Build PE with export data directory
        var builder = new PeBuilder()
            .SetIs64Bit(true)
            .SetCharacteristics(FileCharacteristics.ExecutableImage | FileCharacteristics.Dll);

        var (peData, _, _) = builder.BuildWithDataDirectory(
            DataDirectoryIndex.ExportTable,
            sectionData);

        // Patch the data directory size to only cover the actual export table data,
        // not the full padded section. This prevents function RVAs at offset 0x500
        // from being misdetected as forwarders (which happens when they fall within
        // the export directory's [VA, VA+Size) range).
        int optHeaderOffset = 64 + 4 + 20;
        int dataDirSizeOffset = optHeaderOffset + 112 + (int)DataDirectoryIndex.ExportTable * 8 + 4;
        BitConverter.GetBytes((uint)exportSectionData.Length).CopyTo(peData, dataDirSizeOffset);

        return (peData, exportSectionData);
    }
}
