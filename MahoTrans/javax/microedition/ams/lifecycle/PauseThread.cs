// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.midlet;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Thread = java.lang.Thread;

namespace javax.microedition.ams.lifecycle;

public class PauseThread : Thread
{
    [JavaDescriptor("()V")]
    public new JavaMethodBody run(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall(nameof(CheckState), typeof(bool));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            // if state is true, we silently return.
            b.AppendReturn();
        }

        b.AppendStaticCall<java.lang.System>(nameof(java.lang.System.GetRunningMIDlet), typeof(MIDlet));
        b.AppendVirtcall("pauseApp", typeof(void));
        b.AppendThis();
        b.AppendVirtcall(nameof(Callback), typeof(void));
        b.AppendReturn();
        return b.Build(1, 1);
    }

    public bool CheckState()
    {
        return java.lang.System.GetRunningMIDlet().As<MIDlet>().IsPaused;
    }

    public void Callback()
    {
        java.lang.System.GetRunningMIDlet().As<MIDlet>().IsPaused = true;
        Toolkit.AmsCallbacks?.MidletPaused();
    }
}