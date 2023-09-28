namespace MahoTrans.Runtime;

/// <summary>
/// Exception which is thrown in CLR to pass exception to JVM environment.
/// </summary>
public class JavaThrowable : Exception
{
    public Reference Throwable;

    public JavaThrowable(Reference throwable)
    {
        Throwable = throwable;
    }
}