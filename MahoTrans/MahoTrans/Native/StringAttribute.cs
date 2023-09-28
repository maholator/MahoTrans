namespace MahoTrans.Native;

/// <summary>
/// Marks reference that it contains java string object.
/// </summary>
/// <seealso cref="JavaTypeAttribute"/>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Field)]
public class StringAttribute : Attribute
{
}