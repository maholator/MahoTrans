namespace MahoTrans.Utils;

public static class Utils
{
    public static byte[] Split(this int i)
    {
        return new[] { (byte)((i >> 8) & 0xFF), (byte)(i & 0xFF) };
    }

    public static IEnumerable<Type> EnumerateBaseTypes(this Type type)
    {
        var t = type;
        while (t != null && t != typeof(object))
        {
            yield return t;
            t = t.BaseType;
        }
    }

    /// <summary>
    /// Gets full type name in java style.
    /// </summary>
    /// <param name="t">Type to get name from.</param>
    /// <returns>Name where dots are replaced with slashes.</returns>
    public static string ToJavaName(this Type t) => t.FullName!.Replace('.', '/');
}