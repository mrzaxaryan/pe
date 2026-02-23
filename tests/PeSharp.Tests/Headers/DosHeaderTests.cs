using PeSharp.Tests.Helpers;

namespace PeSharp.Tests.Headers;

public class DosHeaderTests
{
    [Fact]
    public void Parse_ValidMzHeader_ReturnsCorrectMagic()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Equal((ushort)0x5A4D, file.DosHeader.Magic);
        Assert.True(file.DosHeader.IsValid);
    }

    [Fact]
    public void Parse_PeHeaderOffset_PointsToPeSignature()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.Equal(64, file.DosHeader.PeHeaderOffset);
    }

    [Fact]
    public void Parse_InvalidMagic_ThrowsInvalidPeSignatureException()
    {
        var data = new byte[256];
        // Don't set MZ magic
        Assert.Throws<InvalidPeSignatureException>(() => new PeFile(data));
    }

    [Fact]
    public void Parse_FileTooSmall_ThrowsPeTruncatedException()
    {
        var data = new byte[32]; // Less than 64 bytes
        data[0] = 0x4D;
        data[1] = 0x5A;
        Assert.Throws<PeTruncatedException>(() => new PeFile(data));
    }

    [Fact]
    public void Size_Is64()
    {
        Assert.Equal(64, PeSharp.Headers.DosHeader.Size);
    }

    [Fact]
    public void IsValid_WhenMagicIsCorrect_ReturnsTrue()
    {
        var pe = PeBuilder.MinimalPe32Plus();
        using var file = new PeFile(pe);
        Assert.True(file.DosHeader.IsValid);
    }
}
