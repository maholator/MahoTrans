// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.ams.lifecycle;
using MahoTrans.Runtime;

namespace javax.microedition.ams;

/// <summary>
/// Set of utils to manage MIDlet state.
/// </summary>
public static class LifecycleUtils
{
    public static void StartMidlet(string className, Dictionary<string, string> manifest)
    {
        var thread = JvmContext.Jvm!.AllocateObject<StartupThread>();
        thread.MidletClassName = className;
        thread.Manifest = manifest;
        thread.start();
    }

    public static void PauseMidlet()
    {
    }

    public static void ResumeMidlet()
    {
    }

    public static void DestroyMidlet()
    {
    }
}