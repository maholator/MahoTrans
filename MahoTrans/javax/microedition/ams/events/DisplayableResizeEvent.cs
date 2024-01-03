// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.lcdui;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;

namespace javax.microedition.ams.events;

public class DisplayableResizeEvent : Event
{
    [JavaType(typeof(Displayable))] public Reference Target;
    public int Width;
    public int Height;

    [JavaDescriptor("()V")]
    public JavaMethodBody invoke(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetLocalField(nameof(Target), typeof(Displayable));
        b.AppendThis();
        b.AppendGetLocalField(nameof(Width), typeof(int));
        b.AppendThis();
        b.AppendGetLocalField(nameof(Height), typeof(int));
        b.AppendVirtcall(nameof(Displayable.sizeChanged), typeof(void), typeof(int), typeof(int));
        b.AppendReturn();
        return b.Build(3, 1);
    }
}