using MahoTrans.Native;

namespace MahoTrans.Loader;

/// <summary>
/// This is a fallback class which is used by <see cref="ClassCompiler"/> when real interface is not loaded.
/// </summary>
[JavaInterface]
public interface DummyInterface
{
}