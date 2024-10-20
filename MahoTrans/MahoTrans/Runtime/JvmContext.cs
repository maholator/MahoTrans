// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Runtime;

/// <summary>
///     Thread-static slots with references to JVM parts. Wrap this in using construction to use.
/// </summary>
public readonly struct JvmContext : IDisposable
{
    /// <summary>
    ///     <see cref="JvmState" /> object associated with current thread.
    /// </summary>
    [ThreadStatic]
    public static JvmState? Jvm;

    /// <summary>
    ///     Cache of <see cref="Jvm" />.<see cref="JvmState.Toolkit" />.
    /// </summary>
    [ThreadStatic]
    public static ToolkitCollection? Toolkit;

    private readonly JvmState? _prev;

    /// <summary>
    ///     Captures previous context and sets a new one.
    /// </summary>
    /// <param name="jvm">New context to use.</param>
    public JvmContext(JvmState jvm)
    {
        _prev = Jvm;
        Jvm = jvm;
        Toolkit = jvm.Toolkit;
    }

    public void Dispose()
    {
        Jvm = _prev;
        Toolkit = _prev?.Toolkit;
    }
}
