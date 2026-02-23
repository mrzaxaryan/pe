using PeSharp.Tests.Helpers;

namespace PeSharp.Tests.Headers;

public class CoffHeaderTests
{
    [Fact]
    public void Parse_MachineType_Amd64()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .SetMachine(MachineType.Amd64)
            .AddSection(".text", new byte[16])
            .Build();

        using var file = new PeFile(pe);
        Assert.Equal(MachineType.Amd64, file.CoffHeader.Machine);
    }

    [Fact]
    public void Parse_MachineType_I386()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(false)
            .SetMachine(MachineType.I386)
            .AddSection(".text", new byte[16])
            .Build();

        using var file = new PeFile(pe);
        Assert.Equal(MachineType.I386, file.CoffHeader.Machine);
    }

    [Fact]
    public void Parse_Characteristics_DllFlag_IsDllTrue()
    {
        var pe = PeBuilder.MinimalDll64();
        using var file = new PeFile(pe);
        Assert.True(file.CoffHeader.IsDll);
    }

    [Fact]
    public void Parse_Characteristics_NoDllFlag_IsDllFalse()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.False(file.CoffHeader.IsDll);
    }

    [Fact]
    public void Parse_Characteristics_ExecutableFlag_IsExecutableTrue()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.True(file.CoffHeader.IsExecutable);
    }

    [Fact]
    public void Parse_NumberOfSections_MatchesSectionCount()
    {
        var pe = new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[16])
            .AddSection(".data", new byte[32])
            .Build();

        using var file = new PeFile(pe);
        Assert.Equal(2, file.CoffHeader.NumberOfSections);
    }

    [Fact]
    public void Parse_Timestamp_ReturnsDateTimeOffset()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        // Builder uses 0x60000000 as timestamp
        var expected = DateTimeOffset.FromUnixTimeSeconds(0x60000000);
        Assert.Equal(expected, file.CoffHeader.Timestamp);
    }

    [Fact]
    public void Size_Is20()
    {
        Assert.Equal(20, PeSharp.Headers.CoffHeader.Size);
    }
}
