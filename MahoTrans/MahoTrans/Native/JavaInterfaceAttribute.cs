namespace MahoTrans.Native;

/// <summary>
/// Mark an interface with this to make it visible in JVM.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class JavaInterfaceAttribute : Attribute
{
}