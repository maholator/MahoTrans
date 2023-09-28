namespace MahoTrans;

public class JavaLinkageException : Exception
{
    public JavaLinkageException(string? message) : base(message)
    {
    }

    public JavaLinkageException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}