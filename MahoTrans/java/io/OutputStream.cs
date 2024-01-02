// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;

namespace java.io;

public class OutputStream : Object
{
    [InitMethod]
    public new void Init()
    {
        base.Init();
    }

    public void close()
    {
    }

    public void flush()
    {
    }

    public void write(int b)
    {
        throw new AbstractJavaMethodCallError();
    }

    [JavaDescriptor("([B)V")]
    public JavaMethodBody write(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.arraylength);
        b.AppendVirtcall("write", "([BII)V");
        b.AppendReturn();
        return b.Build(3, 2);
    }
}