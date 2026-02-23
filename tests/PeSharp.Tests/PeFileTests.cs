using PeSharp.Tests.Helpers;

namespace PeSharp.Tests;

public class PeFileTests
{
    [Fact]
    public void Constructor_ByteArray_ValidPe32Plus_ParsesSuccessfully()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.NotNull(file);
    }

    [Fact]
    public void Constructor_ByteArray_ValidPe32_ParsesSuccessfully()
    {
        var pe = PeBuilder.MinimalPe32();
        using var file = new PeFile(pe);
        Assert.NotNull(file);
    }

    [Fact]
    public void Constructor_Stream_ValidPe_ParsesSuccessfully()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var stream = new MemoryStream(pe);
        using var file = new PeFile(stream);
        Assert.True(file.Is64Bit);
    }

    [Fact]
    public void Constructor_FilePath_ValidFile_ParsesSuccessfully()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, pe);
            using var file = new PeFile(path);
            Assert.True(file.Is64Bit);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Constructor_ByteArray_InvalidDosSignature_Throws()
    {
        var data = new byte[512];
        Assert.Throws<InvalidPeSignatureException>(() => new PeFile(data));
    }

    [Fact]
    public void Constructor_ByteArray_InvalidPeSignature_Throws()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        // Corrupt PE signature at offset 64
        pe[64] = 0x00;
        pe[65] = 0x00;
        pe[66] = 0x00;
        pe[67] = 0x00;
        Assert.Throws<InvalidPeSignatureException>(() => new PeFile(pe));
    }

    [Fact]
    public void Constructor_ByteArray_EmptyArray_ThrowsPeTruncatedException()
    {
        Assert.Throws<PeTruncatedException>(() => new PeFile(Array.Empty<byte>()));
    }

    [Fact]
    public void Constructor_ByteArray_PeOffsetBeyondFile_ThrowsPeTruncatedException()
    {
        var data = new byte[128];
        data[0] = 0x4D; // M
        data[1] = 0x5A; // Z
        // Set PE offset to point way past end
        BitConverter.GetBytes((uint)0xFFFF).CopyTo(data, 0x3C);
        Assert.Throws<PeTruncatedException>(() => new PeFile(data));
    }

    [Fact]
    public void Is64Bit_Pe32Plus_ReturnsTrue()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.True(file.Is64Bit);
    }

    [Fact]
    public void Is64Bit_Pe32_ReturnsFalse()
    {
        var pe = PeBuilder.MinimalPe32();
        using var file = new PeFile(pe);
        Assert.False(file.Is64Bit);
    }

    [Fact]
    public void IsDll_WithDllCharacteristic_ReturnsTrue()
    {
        var pe = PeBuilder.MinimalDll64();
        using var file = new PeFile(pe);
        Assert.True(file.IsDll);
    }

    [Fact]
    public void IsDll_WithoutDllCharacteristic_ReturnsFalse()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.False(file.IsDll);
    }

    [Fact]
    public void SectionHeaders_CountMatchesCoffHeader()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[16])
            .AddSection(".data", new byte[32])
            .Build();

        using var file = new PeFile(pe);
        Assert.Equal(file.CoffHeader.NumberOfSections, (ushort)file.SectionHeaders.Count);
    }

    [Fact]
    public void ResolveRva_ValidRva_ReturnsFileOffset()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[0x200])
            .Build();

        using var file = new PeFile(pe);
        var section = file.SectionHeaders[0];
        uint? offset = file.ResolveRva(section.VirtualAddress);
        Assert.NotNull(offset);
        Assert.Equal(section.PointerToRawData, offset.Value);
    }

    [Fact]
    public void ResolveRva_InvalidRva_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Null(file.ResolveRva(0xFFFFFFFF));
    }

    [Fact]
    public void GetSectionData_ByName_ReturnsCorrectBytes()
    {
        var sectionData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04 };
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", sectionData)
            .Build();

        using var file = new PeFile(pe);
        var data = file.GetSectionData(".text");
        Assert.True(data.Length >= sectionData.Length);
        Assert.Equal(sectionData, data[..sectionData.Length].ToArray());
    }

    [Fact]
    public void GetSectionData_ByName_NotFound_ThrowsArgumentException()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Throws<ArgumentException>(() => file.GetSectionData(".nonexistent"));
    }

    [Fact]
    public void GetSectionData_ByIndex_ReturnsCorrectBytes()
    {
        var sectionData = new byte[] { 0x11, 0x22, 0x33 };
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", sectionData)
            .Build();

        using var file = new PeFile(pe);
        var data = file.GetSectionData(0);
        Assert.True(data.Length >= sectionData.Length);
        Assert.Equal(sectionData, data[..sectionData.Length].ToArray());
    }

    [Fact]
    public void GetSectionData_ByIndex_Negative_ThrowsArgumentOutOfRangeException()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Throws<ArgumentOutOfRangeException>(() => file.GetSectionData(-1));
    }

    [Fact]
    public void GetSectionData_ByIndex_TooLarge_ThrowsArgumentOutOfRangeException()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Throws<ArgumentOutOfRangeException>(() => file.GetSectionData(100));
    }

    [Fact]
    public void Exports_WhenNoExportTable_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Null(file.Exports);
    }

    [Fact]
    public void Imports_WhenNoImportTable_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Null(file.Imports);
    }

    [Fact]
    public void Resources_WhenNoResourceTable_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Null(file.Resources);
    }

    [Fact]
    public void Relocations_WhenNoRelocationTable_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Null(file.Relocations);
    }

    [Fact]
    public void DebugInfo_WhenNoDebugDirectory_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Null(file.DebugInfo);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        var file = new PeFile(pe);
        file.Dispose();
        file.Dispose(); // Should not throw
    }

    [Fact]
    public void AllConstructors_ProduceSameHeaders()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, pe);

            using var fromBytes = new PeFile(pe);
            using var fromStream = new PeFile(new MemoryStream(pe));
            using var fromFile = new PeFile(path);

            Assert.Equal(fromBytes.DosHeader, fromStream.DosHeader);
            Assert.Equal(fromBytes.DosHeader, fromFile.DosHeader);
            Assert.Equal(fromBytes.CoffHeader, fromStream.CoffHeader);
            Assert.Equal(fromBytes.CoffHeader, fromFile.CoffHeader);
            Assert.Equal(fromBytes.OptionalHeader.Magic, fromFile.OptionalHeader.Magic);
            Assert.Equal(fromBytes.OptionalHeader.ImageBase, fromFile.OptionalHeader.ImageBase);
            Assert.Equal(fromBytes.Is64Bit, fromFile.Is64Bit);
            Assert.Equal(fromBytes.IsDll, fromFile.IsDll);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
