namespace MahoTrans;

public class JavaRuntimeError : Exception
{
    public JavaRuntimeError()
    {
    }

    public JavaRuntimeError(string? message) : base(message)
    {
    }

    public JavaRuntimeError(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}