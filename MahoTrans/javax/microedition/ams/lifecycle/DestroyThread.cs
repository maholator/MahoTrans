// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.midlet;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Thread = java.lang.Thread;

namespace javax.microedition.ams.lifecycle;

public class DestroyThread : Thread
{
    [JavaDescriptor("()V")]
    public new JavaMethodBody run(JavaClass cls)
    {
        JavaMethodBuilder b = new JavaMethodBuilder(cls);
        b.AppendStaticCall<java.lang.System>(nameof(java.lang.System.GetRunningMIDlet), typeof(MIDlet));
        b.Append(JavaOpcode.iconst_1);
        b.AppendVirtcall("destroyApp", "(Z)V");
        b.AppendThis();
        b.AppendVirtcall(nameof(Callback), typeof(void));
        b.AppendReturn();
        return b.Build(2, 1);
    }

    public void Callback()
    {
        Toolkit.AmsCallbacks?.Exited(0);
    }
}
