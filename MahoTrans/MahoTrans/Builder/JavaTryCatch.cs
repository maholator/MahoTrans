namespace MahoTrans.Builder;

/// <summary>
/// Stores try-catch construction.
/// Intended usage:
/// <code>
/// TryBegin:
///     try code
///     goto CatchEnd
/// CatchBegin:
///     catch
/// CatchEnd:
///     following code
/// </code>
/// </summary>
public class JavaTryCatch : IDisposable
{
    public readonly int Exception;
    public readonly JavaLabel TryBegin;
    public readonly JavaLabel CatchBegin;
    public readonly JavaLabel CatchEnd;
    public readonly JavaMethodBuilder Builder;

    public JavaTryCatch(JavaMethodBuilder builder, int exception, JavaLabel tryBegin, JavaLabel catchBegin,
        JavaLabel catchEnd)
    {
        Exception = exception;
        TryBegin = tryBegin;
        CatchBegin = catchBegin;
        CatchEnd = catchEnd;
        Builder = builder;
    }

    public void CatchSection()
    {
        Builder.AppendGoto(JavaOpcode.@goto, CatchEnd);
        Builder.BringLabel(CatchBegin);
    }

    public void Dispose()
    {
        Builder.BringLabel(CatchEnd);
    }
}