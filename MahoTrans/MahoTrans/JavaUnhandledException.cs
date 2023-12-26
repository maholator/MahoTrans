using MahoTrans.Runtime;

namespace MahoTrans;

public class JavaUnhandledException : JavaRuntimeError
{
    public JavaUnhandledException(string message, JavaThrowable innerException) : base(message, innerException)
    {
    }
}