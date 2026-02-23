using PeSharp.Tests.Helpers;

namespace PeSharp.Tests;

public class RvaResolverTests
{
    [Fact]
    public void ResolveRva_RvaInSection_ReturnsCorrectFileOffset()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[0x200])
            .Build();

        using var file = new PeFile(pe);
        var section = file.SectionHeaders[0];

        // RVA = VirtualAddress + 0x50
        uint rva = section.VirtualAddress + 0x50;
        uint? result = file.ResolveRva(rva);

        Assert.NotNull(result);
        // Formula: rva - VirtualAddress + PointerToRawData
        Assert.Equal(section.PointerToRawData + 0x50, result.Value);
    }

    [Fact]
    public void ResolveRva_RvaInSecondSection_ReturnsCorrectFileOffset()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[0x200])
            .AddSection(".data", new byte[0x200])
            .Build();

        using var file = new PeFile(pe);
        var section = file.SectionHeaders[1];

        uint rva = section.VirtualAddress + 0x10;
        uint? result = file.ResolveRva(rva);

        Assert.NotNull(result);
        Assert.Equal(section.PointerToRawData + 0x10, result.Value);
    }

    [Fact]
    public void ResolveRva_RvaNotInAnySection_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);

        uint? result = file.ResolveRva(0xFFFFFFFF);
        Assert.Null(result);
    }

    [Fact]
    public void ResolveRva_RvaAtSectionStart_ReturnsPointerToRawData()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[0x200])
            .Build();

        using var file = new PeFile(pe);
        var section = file.SectionHeaders[0];

        uint? result = file.ResolveRva(section.VirtualAddress);
        Assert.NotNull(result);
        Assert.Equal(section.PointerToRawData, result.Value);
    }

    [Fact]
    public void ResolveRva_RvaZero_ReturnsNull()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);

        // RVA 0 is before any section (sections start at 0x1000)
        uint? result = file.ResolveRva(0);
        Assert.Null(result);
    }

    [Fact]
    public void ResolveRva_RvaPastSectionEnd_ReturnsNull()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[0x100])
            .Build();

        using var file = new PeFile(pe);
        var section = file.SectionHeaders[0];
        uint end = section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData);

        uint? result = file.ResolveRva(end);
        Assert.Null(result);
    }
}
