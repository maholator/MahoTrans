// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.ams.lifecycle;
using MahoTrans.Native;
using Object = java.lang.Object;

namespace javax.microedition.ams;

/// <summary>
/// Set of utils to manage MIDlet state.
/// </summary>
public class LifecycleUtils : Object
{
    [JavaIgnore]
    public static void StartMidlet(string className, Dictionary<string, string> manifest)
    {
        var thread = Jvm.AllocateObject<StartupThread>();
        thread.MidletClassName = className;
        thread.Manifest = manifest;
        thread.start();
    }

    [JavaIgnore]
    public static void PauseMidlet()
    {
        var thread = Jvm.AllocateObject<PauseThread>();
        thread.start();
    }

    [JavaIgnore]
    public static void ResumeMidlet()
    {
        var thread = Jvm.AllocateObject<ResumeThread>();
        thread.start();
    }

    [JavaIgnore]
    public static void DestroyMidlet()
    {
        var thread = Jvm.AllocateObject<DestroyThread>();
        thread.start();
    }
}