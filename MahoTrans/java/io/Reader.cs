// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;

namespace java.io;

public class Reader : Object
{
    [JavaDescriptor("([C)I")]
    public JavaMethodBody read___buf(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.arraylength);
        b.AppendVirtcall("read", "([CII)I");
        b.AppendReturnInt();
        return b.Build(4, 2);
    }
}