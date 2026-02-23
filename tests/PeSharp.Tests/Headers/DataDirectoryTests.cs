using PeSharp.Headers;

namespace PeSharp.Tests.Headers;

public class DataDirectoryTests
{
    [Fact]
    public void IsEmpty_BothZero_ReturnsTrue()
    {
        var dir = new DataDirectory(0, 0);
        Assert.True(dir.IsEmpty);
    }

    [Fact]
    public void IsEmpty_VirtualAddressNonZero_ReturnsFalse()
    {
        var dir = new DataDirectory(0x1000, 0);
        Assert.False(dir.IsEmpty);
    }

    [Fact]
    public void IsEmpty_SizeNonZero_ReturnsFalse()
    {
        var dir = new DataDirectory(0, 0x100);
        Assert.False(dir.IsEmpty);
    }

    [Fact]
    public void StructSize_Is8()
    {
        Assert.Equal(8, DataDirectory.StructSize);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = new DataDirectory(0x1000, 0x200);
        var b = new DataDirectory(0x1000, 0x200);
        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var a = new DataDirectory(0x1000, 0x200);
        var b = new DataDirectory(0x2000, 0x200);
        Assert.NotEqual(a, b);
    }
}
