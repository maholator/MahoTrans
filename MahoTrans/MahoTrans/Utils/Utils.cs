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

    public static int NextKey<T>(this Dictionary<int, T> dict, int defaultKey = 0)
    {
        if (dict.Count == 0)
            return defaultKey;
        return dict.Keys.Max() + 1;
    }

    public static int Push<T>(this Dictionary<int, T> dict, T value, int defaultKey = 0)
    {
        var k = dict.NextKey(defaultKey);
        dict[k] = value;
        return k;
    }

    public static void Enqueue<T>(this Queue<T> queue, IEnumerable<T> list)
    {
        foreach (var val in list)
        {
            queue.Enqueue(val);
        }
    }
}