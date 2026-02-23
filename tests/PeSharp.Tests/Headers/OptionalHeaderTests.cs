using PeSharp.Tests.Helpers;

namespace PeSharp.Tests.Headers;

public class OptionalHeaderTests
{
    [Fact]
    public void Parse_Pe32Plus_IsPe32PlusTrue()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.True(file.OptionalHeader.IsPe32Plus);
        Assert.False(file.OptionalHeader.IsPe32);
    }

    [Fact]
    public void Parse_Pe32_IsPe32True()
    {
        var pe = PeBuilder.MinimalPe32();
        using var file = new PeFile(pe);
        Assert.True(file.OptionalHeader.IsPe32);
        Assert.False(file.OptionalHeader.IsPe32Plus);
    }

    [Fact]
    public void Parse_Pe32Plus_ImageBase_Is64Bit()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Equal(0x140000000ul, file.OptionalHeader.ImageBase);
    }

    [Fact]
    public void Parse_Pe32_ImageBase_Is32Bit()
    {
        var pe = PeBuilder.MinimalPe32();
        using var file = new PeFile(pe);
        Assert.Equal(0x00400000ul, file.OptionalHeader.ImageBase);
    }

    [Fact]
    public void Parse_Pe32_HasBaseOfData()
    {
        var pe = PeBuilder.MinimalPe32();
        using var file = new PeFile(pe);
        Assert.Equal(0x2000u, file.OptionalHeader.BaseOfData);
    }

    [Fact]
    public void Parse_Pe32Plus_BaseOfDataIsZero()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Equal(0u, file.OptionalHeader.BaseOfData);
    }

    [Fact]
    public void Parse_DataDirectories_CorrectCount()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Equal(16, file.OptionalHeader.DataDirectories.Count);
    }

    [Fact]
    public void Parse_DataDirectories_ClampedTo16()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .SetNumDataDirectories(100) // Will be built with 100 but clamped by parser
            .AddSection(".text", new byte[16])
            .Build();

        // Patch NumberOfRvaAndSizes to 100 in the raw bytes
        // It's at optional header offset + (is64 ? 108 : 92)
        int optHeaderOffset = 64 + 4 + 20; // 88
        BitConverter.GetBytes((uint)100).CopyTo(pe, optHeaderOffset + 108);

        using var file = new PeFile(pe);
        Assert.Equal(16, file.OptionalHeader.DataDirectories.Count);
    }

    [Fact]
    public void Parse_DataDirectories_LessThan16()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .SetNumDataDirectories(5)
            .AddSection(".text", new byte[16])
            .Build();

        using var file = new PeFile(pe);
        Assert.Equal(5, file.OptionalHeader.DataDirectories.Count);
    }

    [Fact]
    public void Parse_Subsystem_WindowsCui()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .SetSubsystem(Subsystem.WindowsCui)
            .AddSection(".text", new byte[16])
            .Build();

        using var file = new PeFile(pe);
        Assert.Equal(Subsystem.WindowsCui, file.OptionalHeader.Subsystem);
    }

    [Fact]
    public void Parse_SectionAlignment_IsCorrect()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Equal(0x1000u, file.OptionalHeader.SectionAlignment);
    }

    [Fact]
    public void Parse_FileAlignment_IsCorrect()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Equal(0x200u, file.OptionalHeader.FileAlignment);
    }
}
