// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.midlet;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Thread = java.lang.Thread;

namespace javax.microedition.ams.lifecycle;

/// <summary>
///     Thread that can start a MIDlet. Allocates the object, calls constructor and startApp.
///     Assign <see cref="MidletClassName" /> and <see cref="Manifest" /> fields and start it as a regular java thread.
/// </summary>
public class StartupThread : Thread
{
    /// <summary>
    /// Name of MIDlet class. This will be instantiated.
    /// </summary>
    [JavaIgnore] public string MidletClassName = null!;

    /// <summary>
    /// MIDlet's manifest. Will be passed to <see cref="MIDlet.Properties" />.
    /// </summary>
    [JavaIgnore] public Dictionary<string, string> Manifest = null!;

    [JavaDescriptor("()V")]
    public new JavaMethodBody run(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall(nameof(AllocMidlet), typeof(Reference));
        b.Append(JavaOpcode.dup);
        b.AppendVirtcall("<init>", typeof(void));
        b.AppendVirtcall("startApp", typeof(void));
        b.AppendReturn();
        return b.Build(2, 1);
    }

    /// <summary>
    /// Allocates MIDlet object.
    /// </summary>
    /// <returns>MIDlet object. Call its init method and start it.</returns>
    public Reference AllocMidlet()
    {
        var midlet = Jvm.AllocateObject(Jvm.Classes[MidletClassName]);
        Jvm.Resolve<MIDlet>(midlet).Properties = Manifest;
        Jvm.MidletObject = midlet;
        return midlet;
    }
}