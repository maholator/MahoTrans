// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.io;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Newtonsoft.Json;

namespace java.lang;

public class System : Object
{
    [JsonProperty] [JavaType(typeof(PrintStream))]
    public static Reference @out;

    [JsonProperty] [JavaType(typeof(PrintStream))]
    public static Reference err;

    [ClassInit]
    public static void Clinit()
    {
        var outPrinter = Jvm.AllocateObject<PrintStream>();
        outPrinter.Init(Jvm.AllocateObject<StdOut>().This);
        @out = outPrinter.This;

        var errPrinter = Jvm.AllocateObject<PrintStream>();
        errPrinter.Init(Jvm.AllocateObject<StdErr>().This);
        err = errPrinter.This;
    }

    public static int identityHashCode(Reference x)
    {
        var obj = Jvm.ResolveObject(x);
        return obj.HeapAddress;
    }

    [return: String]
    public static Reference getProperty([String] Reference key)
    {
        var keyStr = Jvm.ResolveString(key);
        var val = Toolkit.System.GetProperty(keyStr);
        if (val == null)
            return Reference.Null;
        return Jvm.InternalizeString(val);
    }

    public static long currentTimeMillis() => Toolkit.Clock.GetCurrentMs(Jvm.CycleNumber);

    public static void gc()
    {
        Jvm.RunGarbageCollector();
    }

    public static void arraycopy(Reference src, int src_position, Reference dst, int dst_position, int length)
    {
        var arr1 = Jvm.Resolve<Array>(src).BaseValue;
        var arr2 = Jvm.Resolve<Array>(dst).BaseValue;
        global::System.Array.Copy(arr1, src_position, arr2, dst_position, length);
    }
}