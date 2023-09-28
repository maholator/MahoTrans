using System.Reflection;

namespace MahoTrans.Runtime.Types;

public class Method
{
    public readonly MethodFlags Flags;
    public readonly NameDescriptor Descriptor;
    public JavaAttribute[] Attributes = Array.Empty<JavaAttribute>();
    private object _methodBody = null!;
    public int BridgeNumber;
    public Action<Frame>? Bridge;
    public readonly JavaClass Class;

    public Method(NameDescriptor descriptor, MethodFlags flags, JavaClass @class)
    {
        Descriptor = descriptor;
        Flags = flags;
        Class = @class;
    }

    public bool IsStatic => Flags.HasFlag(MethodFlags.Static);
    public bool IsNative => Flags.HasFlag(MethodFlags.Native);

    public MethodInfo NativeBody
    {
        get => (MethodInfo)_methodBody;
        set => _methodBody = value;
    }

    public JavaMethodBody JavaBody
    {
        get => (JavaMethodBody)_methodBody;
        set
        {
            value.Method = this;
            _methodBody = value;
        }
    }

    public override string ToString()
    {
        var s = $"{Descriptor}";

        if (IsNative)
            return $"{s} (native)";
        return s;
    }
}