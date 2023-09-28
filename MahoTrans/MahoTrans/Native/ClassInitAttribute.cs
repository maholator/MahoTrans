namespace MahoTrans.Native;

/// <summary>
/// Mark a method with this to make it class initializer (static block). Method's name will be discarded. Method must return void.
/// </summary>
/// <seealso cref="InitMethodAttribute"/>
[AttributeUsage(AttributeTargets.Method)]
public class ClassInitAttribute : Attribute
{
}