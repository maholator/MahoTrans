using System.Reflection;
using MahoTrans.Utils;

namespace MahoTrans.Runtime.Types;

public class Method : IDisposable
{
    public readonly MethodFlags Flags;
    public readonly NameDescriptor Descriptor;
    public JavaAttribute[] Attributes = Array.Empty<JavaAttribute>();
    private object _methodBody = null!;
    public int BridgeNumber;
    public Action<Frame>? Bridge;
    public readonly JavaClass Class;

    /// <summary>
    /// Number of method's self args. If method is non-static, this does not include "this" arg.
    /// </summary>
    public readonly int ArgsCount;

    public Method(NameDescriptor descriptor, MethodFlags flags, JavaClass @class)
    {
        Descriptor = descriptor;
        Flags = flags;
        Class = @class;
        ArgsCount = DescriptorUtils.ParseMethodArgsCount(descriptor.Descriptor);
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
        var s = $"{Class.Name}::{Descriptor}";

        if (IsNative)
            return $"{s} (native)";
        return s;
    }

    public uint GetSnapshotHash()
    {
        return Descriptor.GetSnapshotHash() ^ ((_methodBody as JavaMethodBody)?.GetSnapshotHash() ?? 0U) ^ (uint)Flags;
    }

    public void Dispose()
    {
        Attributes = null!;
        _methodBody = null!;
        Bridge = null!;
    }
}