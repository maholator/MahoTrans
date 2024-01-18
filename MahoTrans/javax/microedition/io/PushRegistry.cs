﻿// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.util;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.io;

public class PushRegistry : Object
{
    [JavaDescriptor("(Z)[Ljava/lang/String;")]
    public static Reference listConnections(bool b)
    {
        return Jvm.AllocateArray(Array.Empty<Reference>(), "[Ljava/lang/String;");
    }
}