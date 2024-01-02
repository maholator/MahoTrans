// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;

namespace javax.microedition.ams.events;

public class CanvasPointerUpEvent : CanvasPointerEvent
{
    [JavaDescriptor("()V")]
    public JavaMethodBody invoke(JavaClass cls) => GenerateBridge(cls, "pointerReleased");
}