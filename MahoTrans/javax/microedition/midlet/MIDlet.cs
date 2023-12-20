using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.midlet;

public class MIDlet : Object
{
    [JavaIgnore] public Dictionary<string, string> Properties = null!;

    public Reference Display;

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
        Toolkit.Ams.DestroyMidlet();
    }

    public bool platformRequest([String] Reference url)
    {
        Toolkit.Ams.PlatformRequest(Jvm.ResolveString(url));
        return false;
    }
}