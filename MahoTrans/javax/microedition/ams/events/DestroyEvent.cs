using javax.microedition.midlet;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;

namespace javax.microedition.ams.events;

public class DestroyEvent : Event
{
    [JavaType(typeof(MIDlet))] public Reference Midlet;

    [JavaDescriptor("()V")]
    public JavaMethodBody invoke(JavaClass cls)
    {
        JavaMethodBuilder b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetLocalField("Midlet", typeof(MIDlet));
        b.Append(JavaOpcode.iconst_1);
        b.AppendVirtcall("destroyApp", "(Z)V");
        b.AppendThis();
        b.AppendGetLocalField("Midlet", typeof(MIDlet));
        b.AppendVirtcall("notifyDestroyed", "()V");
        b.AppendReturn();
        return b.Build(2, 1);
    }
}