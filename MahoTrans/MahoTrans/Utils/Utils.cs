namespace MahoTrans.Utils;

public static class Utils
{
    /// <summary>
    /// Returns byte[2] with 2 low bytes of passed number. I.e. for 258 returns [0x01,0x02].
    /// </summary>
    /// <param name="i">Number to split.</param>
    /// <returns>Splitted bytes.</returns>
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

    /// <summary>
    /// Gets full type name as descriptor, i.e. Lpkg/obj;
    /// </summary>
    /// <param name="t">Type to get name from.</param>
    /// <returns>Name with dots replaced by slashes int L; form.</returns>
    public static string ToJavaDescriptor(this Type t) => $"L{t.ToJavaName()};";
}