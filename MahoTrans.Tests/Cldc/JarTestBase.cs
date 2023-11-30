using MahoTrans.Testing;
using Object = java.lang.Object;

namespace MahoTrans.Tests.Cldc;

public abstract class JarTestBase
{
    public abstract string ClassName { get; }

    public JavaTest Load(string method)
    {
        using var file = GetType().Assembly.GetManifestResourceStream(GetType(), "MaholatorTests.jar");
        return new JavaTest(file, ClassName, method);
    }

    protected static void DetachHeap() => Object.JvmUnchecked = null;
}