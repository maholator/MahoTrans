// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.midlet;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Thread = java.lang.Thread;

namespace javax.microedition.ams;

public class MidletStartup : Thread
{
    [JavaIgnore] public string MidletClassName;
    [JavaIgnore] public Dictionary<string, string> Manifest;

    [JavaDescriptor("()V")]
    public JavaMethodBody run(JavaClass cls)
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

    public Reference AllocMidlet()
    {
        var midlet = Jvm.AllocateObject(Jvm.Classes[MidletClassName]);
        Jvm.Resolve<MIDlet>(midlet).Properties = Manifest;
        Jvm.MidletObject = midlet;
        return midlet;
    }
}