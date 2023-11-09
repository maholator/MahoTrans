using JetBrains.Annotations;

namespace MahoTrans.Native;

/// <summary>
/// Specifies full JVM descriptor for a CLR method. For example, "(IIJ)Z". If present, actual parameter types will be ignored.
/// </summary>
/// <seealso cref="JavaTypeAttribute"/>
[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public class JavaDescriptorAttribute : Attribute
{
    public string Descriptor;

    public JavaDescriptorAttribute(string descriptor)
    {
        Descriptor = descriptor;
    }
}