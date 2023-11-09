using JetBrains.Annotations;

namespace MahoTrans.Native;

[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public class StaticFieldsAnnouncerAttribute : Attribute
{
}