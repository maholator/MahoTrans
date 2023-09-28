namespace MahoTrans.Native;

/// <summary>
/// Mark a method with this to make it class instance initializer. Method's name will be discarded. Method must return void.
/// </summary>
/// <seealso cref="ClassInitAttribute"/>
[AttributeUsage(AttributeTargets.Method)]
public class InitMethodAttribute : Attribute
{
}