using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;

namespace javax.microedition.ams.events;

public class CanvasPointerDragEvent : CanvasPointerEvent
{
    [JavaDescriptor("()V")]
    public JavaMethodBody invoke(JavaClass cls) => GenerateBridge(cls, "pointerDragged");
}