using PeSharp.Tests.Helpers;

namespace PeSharp.Tests.Headers;

public class SectionHeaderTests
{
    [Fact]
    public void Parse_SingleSection_ReturnsCorrectFields()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[64], SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute | SectionCharacteristics.ContainsCode)
            .Build();

        using var file = new PeFile(pe);
        Assert.Single(file.SectionHeaders);

        var section = file.SectionHeaders[0];
        Assert.Equal(".text", section.Name);
        Assert.Equal(64u, section.VirtualSize);
        Assert.Equal(0x1000u, section.VirtualAddress);
        Assert.True(section.SizeOfRawData > 0);
        Assert.True(section.PointerToRawData > 0);
    }

    [Fact]
    public void ParseAll_MultipleSections_ReturnsCorrectCount()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[16])
            .AddSection(".data", new byte[32])
            .AddSection(".rdata", new byte[48])
            .Build();

        using var file = new PeFile(pe);
        Assert.Equal(3, file.SectionHeaders.Count);
        Assert.Equal(".text", file.SectionHeaders[0].Name);
        Assert.Equal(".data", file.SectionHeaders[1].Name);
        Assert.Equal(".rdata", file.SectionHeaders[2].Name);
    }

    [Fact]
    public void ContainsRva_InsideSection_ReturnsTrue()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[0x100])
            .Build();

        using var file = new PeFile(pe);
        var section = file.SectionHeaders[0];
        Assert.True(section.ContainsRva(section.VirtualAddress + 0x50));
    }

    [Fact]
    public void ContainsRva_AtExactStart_ReturnsTrue()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[0x100])
            .Build();

        using var file = new PeFile(pe);
        var section = file.SectionHeaders[0];
        Assert.True(section.ContainsRva(section.VirtualAddress));
    }

    [Fact]
    public void ContainsRva_BeforeSection_ReturnsFalse()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[0x100])
            .Build();

        using var file = new PeFile(pe);
        var section = file.SectionHeaders[0];
        Assert.False(section.ContainsRva(section.VirtualAddress - 1));
    }

    [Fact]
    public void ContainsRva_AfterSection_ReturnsFalse()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[0x100])
            .Build();

        using var file = new PeFile(pe);
        var section = file.SectionHeaders[0];
        uint end = section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData);
        Assert.False(section.ContainsRva(end));
    }

    [Fact]
    public void Characteristics_FlagsDecodedCorrectly()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[16], SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute | SectionCharacteristics.ContainsCode)
            .Build();

        using var file = new PeFile(pe);
        var chars = file.SectionHeaders[0].Characteristics;
        Assert.True(chars.HasFlag(SectionCharacteristics.MemRead));
        Assert.True(chars.HasFlag(SectionCharacteristics.MemExecute));
        Assert.True(chars.HasFlag(SectionCharacteristics.ContainsCode));
    }

    [Fact]
    public void Size_Is40()
    {
        Assert.Equal(40, PeSharp.Headers.SectionHeader.Size);
    }
}
