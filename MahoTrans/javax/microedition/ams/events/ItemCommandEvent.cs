// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.lcdui;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;

namespace javax.microedition.ams.events;

public class ItemCommandEvent : Event
{
    [JavaType(typeof(Item))]
    public Reference Target;

    [JavaType(typeof(Command))]
    public Reference Command;

    [JavaDescriptor("()V")]
    public JavaMethodBody invoke(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetLocalField(nameof(Target), typeof(Item));
        b.Append(JavaOpcode.dup);
        b.Append(JavaOpcode.astore_1);
        using (b.AppendGoto(JavaOpcode.ifnonnull))
        {
            b.AppendReturn();
        }

        b.Append(JavaOpcode.aload_1);
        b.AppendGetField(nameof(Item.Listener), typeof(ItemCommandListener), typeof(Item));
        b.Append(JavaOpcode.dup);
        using (b.AppendGoto(JavaOpcode.ifnonnull))
        {
            b.AppendReturn();
        }

        b.AppendThis();
        b.AppendGetLocalField(nameof(Command), typeof(Command));
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall(nameof(ItemCommandListener.commandAction), typeof(void), typeof(Command), typeof(Item));
        b.AppendReturn();
        return b.Build(3, 2);
    }
}
