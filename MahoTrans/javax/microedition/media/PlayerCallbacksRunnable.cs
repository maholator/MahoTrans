// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;
using String = java.lang.String;

namespace javax.microedition.media;

public class PlayerCallbacksRunnable : Object, Runnable
{
    [JavaType(typeof(Player))]
    public Reference Player;

    [String]
    public Reference EventName;

    public Reference Args;
    public Reference Targets;

    [InitMethod]
    public void Init([JavaType(typeof(Player))] Reference player, [String] Reference eventName, Reference args,
        Reference targets)
    {
        base.Init();
        Player = player;
        EventName = eventName;
        Args = args;
        Targets = targets;
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody run(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        // this > targets > i > player

        b.AppendThis();
        b.AppendGetLocalField(nameof(Targets), typeof(Object));
        b.Append(JavaOpcode.astore_1);

        b.AppendThis();
        b.AppendGetLocalField(nameof(Player), typeof(Player));
        b.Append(JavaOpcode.astore_3);

        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.istore_2);

        using (var l = b.BeginLoop(JavaOpcode.if_icmplt))
        {
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.aaload);
            // listener
            b.Append(JavaOpcode.aload_3);
            // listener > player
            b.AppendThis();
            b.AppendGetLocalField(nameof(EventName), typeof(String));
            // listener > player > eventName
            b.AppendThis();
            b.AppendGetLocalField(nameof(Args), typeof(Object));
            // listener > player > eventName > args
            b.AppendVirtcall("playerUpdate", typeof(void), typeof(Player), typeof(String), typeof(Object));

            b.AppendInc(2, 1); // i++

            l.ConditionSection(); // i < targets.length
            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.arraylength);
        }

        b.Append(JavaOpcode.aload_3);
        b.AppendVirtcall(nameof(PlayerImpl.ListenersThreadExited), typeof(void));

        b.AppendReturn();

        return b.Build(4, 4);
    }
}
