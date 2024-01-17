// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.util;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;
using IOException = java.io.IOException;

namespace javax.microedition.io.file;

public class FileSystemRegistry : Object
{
    [JavaDescriptor("()Ljava/util/Enumeration;")]
    public static Reference listRoots()
    {
        Jvm.Throw<IOException>();
        return default;
    }
}