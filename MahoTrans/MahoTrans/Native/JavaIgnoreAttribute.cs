namespace MahoTrans.Native;

/// <summary>
/// Mark something with this attribute to make it invisible for JVM.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Method)]
public class JavaIgnoreAttribute : Attribute
{
}