using MahoTrans.Runtime.Types;

namespace MahoTrans.Runtime;

public class FieldPointer
{
    public readonly Action<Frame> Bridge;
    public readonly JavaClass Class;

    public FieldPointer(Action<Frame> bridge, JavaClass @class)
    {
        Bridge = bridge;
        Class = @class;
    }
}