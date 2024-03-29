// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.lcdui;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;

namespace javax.microedition.ams.events;

public class ChangeNotifyEvent : Event
{
    [JavaType(typeof(Form))]
    public Reference Target;

    [JavaType(typeof(Item))]
    public Reference Item;

    [JavaDescriptor("()V")]
    public JavaMethodBody invoke(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetLocalField(nameof(Target), typeof(Form));
        b.Append(JavaOpcode.dup);
        using (b.AppendGoto(JavaOpcode.ifnonnull))
        {
            b.AppendReturn();
        }

        b.AppendGetField(nameof(Form.StateListener), typeof(ItemStateListener), typeof(Form));
        b.Append(JavaOpcode.dup);
        using (b.AppendGoto(JavaOpcode.ifnonnull))
        {
            b.AppendReturn();
        }

        b.AppendGetLocalField(nameof(Item), typeof(Item));
        b.AppendVirtcall(nameof(ItemStateListener.itemStateChanged), typeof(void), typeof(Item));
        b.AppendReturn();
        return b.Build(2, 1);
    }
}
