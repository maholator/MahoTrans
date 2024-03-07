// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.io;

public class PushRegistry : Object
{
    [JavaDescriptor("(Z)[Ljava/lang/String;")]
    public static Reference listConnections(bool b)
    {
        return Jvm.WrapReferenceArray(Array.Empty<Reference>(), "[Ljava/lang/String;");
    }
}
