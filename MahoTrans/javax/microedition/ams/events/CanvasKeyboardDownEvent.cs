using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;

namespace javax.microedition.ams.events;

public class CanvasKeyboardDownEvent : CanvasKeyboardEvent
{
    [JavaDescriptor("()V")]
    public JavaMethodBody invoke(JavaClass cls) => GenerateBridge(cls, "keyPressed");
}