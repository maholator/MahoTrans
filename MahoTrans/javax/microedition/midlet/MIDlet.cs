// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Errors;
using Object = java.lang.Object;

namespace javax.microedition.midlet;

public class MIDlet : Object
{
    [JavaIgnore] public Dictionary<string, string> Properties = null!;

    public Reference Display;

    /// <summary>
    /// This is true if MIDlet is in paused state.
    /// </summary>
    [JavaIgnore] public bool IsPaused;

    [InitMethod]
    public new void Init()
    {
        if (Properties == null!)
        {
            throw new JavaRuntimeError(
                "Frontend must explicitly set properties map before attempting to run the midlet.");
        }
    }

    [return: String]
    public Reference getAppProperty([String] Reference r)
    {
        if (Properties.TryGetValue(Jvm.ResolveString(r), out var val))
            return Jvm.InternalizeString(val);

        return Reference.Null;
    }

    public void notifyDestroyed()
    {
        java.lang.System.exit(0);
    }

    public void notifyPaused()
    {
        if (IsPaused)
            return;

        IsPaused = true;
        Toolkit.AmsCallbacks?.MidletPaused();
    }

    public void resumeRequest()
    {
        if (!IsPaused)
            return;

        Toolkit.AmsCallbacks?.AskForResume();
    }

    public bool platformRequest([String] Reference url)
    {
        Toolkit.AmsCallbacks?.PlatformRequest(Jvm.ResolveString(url));
        return false;
    }
}