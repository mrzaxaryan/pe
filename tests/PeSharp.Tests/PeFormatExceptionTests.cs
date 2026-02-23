namespace PeSharp.Tests;

public class PeFormatExceptionTests
{
    [Fact]
    public void PeFormatException_MessagePreserved()
    {
        var ex = new PeFormatException("test message");
        Assert.Equal("test message", ex.Message);
    }

    [Fact]
    public void PeFormatException_InnerExceptionPreserved()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new PeFormatException("outer", inner);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void PeFormatException_IsException()
    {
        Assert.IsAssignableFrom<Exception>(new PeFormatException("test"));
    }

    [Fact]
    public void InvalidPeSignatureException_IsPeFormatException()
    {
        Assert.IsAssignableFrom<PeFormatException>(new InvalidPeSignatureException("test"));
    }

    [Fact]
    public void PeTruncatedException_IsPeFormatException()
    {
        Assert.IsAssignableFrom<PeFormatException>(new PeTruncatedException("test"));
    }
}
