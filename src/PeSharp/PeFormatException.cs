namespace PeSharp;

public class PeFormatException : Exception
{
    public PeFormatException(string message) : base(message) { }
    public PeFormatException(string message, Exception inner) : base(message, inner) { }
}

public class InvalidPeSignatureException : PeFormatException
{
    public InvalidPeSignatureException(string message) : base(message) { }
}

public class PeTruncatedException : PeFormatException
{
    public PeTruncatedException(string message) : base(message) { }
}
