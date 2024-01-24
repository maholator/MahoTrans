// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        try
        {
            var drives = Directory.GetLogicalDrives();
            Reference[] r = new Reference[drives.Length];
            for (int i = 0; i < r.Length; i++)
                r[i] = Jvm.AllocateString("file:///" + drives[i].Replace('\\', '/'));
            var enumerator = Jvm.AllocateObject<ArrayEnumerator>();
            enumerator.Value = r;
            enumerator.Init();
            return enumerator.This;
        }
        catch
        {
            Jvm.Throw<IOException>();
            return default;
        }
    }
}