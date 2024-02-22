// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Security.Cryptography;
using System.Text;
using MahoTrans.Abstractions;

namespace MahoTrans.Utils;

/// <summary>
///     Various helper methods. To be sorted/splitted.
/// </summary>
public static class Utils
{
    /// <summary>
    ///     Returns byte[2] with 2 low bytes of passed number. I.e. for 258 returns [0x01,0x02].
    /// </summary>
    /// <param name="i">Number to split.</param>
    /// <returns>Splitted bytes.</returns>
    public static byte[] Split(this int i)
    {
        return new[] { (byte)((i >> 8) & 0xFF), (byte)(i & 0xFF) };
    }

    /// <summary>
    ///     Returns byte[2] with bytes of passed number. I.e. for 258 returns [0x01,0x02].
    /// </summary>
    /// <param name="s">Number to split.</param>
    /// <returns>Splitted bytes.</returns>
    public static byte[] Split(this short s)
    {
        return new[] { (byte)((s >> 8) & 0xFF), (byte)(s & 0xFF) };
    }

    /// <summary>
    ///     Enumerates base types of passed type. First returns type itself.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>Enumerator of base types.</returns>
    public static IEnumerable<Type> EnumerateBaseTypes(this Type type)
    {
        var t = type;

        while (t != null && t != typeof(object))
        {
            yield return t;
            t = t.BaseType;
        }
    }

    public static int NextKey<T>(this Dictionary<int, T> dict, int defaultKey = 1)
    {
        if (dict.Count == 0)
            return defaultKey;
        return dict.Keys.Max() + 1;
    }

    public static int Push<T>(this Dictionary<int, T> dict, T value, int defaultKey = 1)
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

    public static void Enqueue<T>(this Queue<T> queue, T[] list)
    {
        foreach (var val in list)
        {
            queue.Enqueue(val);
        }
    }

    /// <summary>
    ///     Allows to call string.Join in functional style.
    /// </summary>
    /// <param name="sequence">Sequence to join.</param>
    /// <param name="separator">Join separator.</param>
    /// <typeparam name="T">Type of values.</typeparam>
    /// <returns>Joined string.</returns>
    public static string Join<T>(this IEnumerable<T> sequence, string separator = "")
    {
        return string.Join(separator, sequence);
    }

    /// <summary>
    ///     Pops and discards N values from stack.
    /// </summary>
    /// <param name="stack">Stack to pop from.</param>
    /// <param name="count">Count of values to pop.</param>
    /// <typeparam name="T">Type of elements in stack.</typeparam>
    public static void Pop<T>(this Stack<T> stack, int count)
    {
        for (int i = 0; i < count; i++)
        {
            stack.Pop();
        }
    }

    public static string ComputeFileMD5(this string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);

        var ba = md5.ComputeHash(stream);
        return Convert.ToHexString(ba);
    }

    public static string ComputeMD5(this string input)
    {
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(input)));
    }

    public static string ComputeFileSHA1(this string filePath)
    {
        using var sha = SHA1.Create();
        using var stream = File.OpenRead(filePath);

        var ba = sha.ComputeHash(stream);
        return Convert.ToHexString(ba);
    }

    public static string ComputeSHA1(this string input)
    {
        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(input)));
    }

    public static string AsBase64(this string str)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
    }

    public static string DecodeBase64(this string str)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(str));
    }

    public static MTLogLevel GetSeverity(this LoadIssueType type)
    {
        return type switch
        {
            LoadIssueType.MissingClassAccess => MTLogLevel.Warning,
            LoadIssueType.MissingMethodAccess => MTLogLevel.Error,
            LoadIssueType.MissingFieldAccess => MTLogLevel.Error,
            LoadIssueType.InvalidConstant => MTLogLevel.Error,
            LoadIssueType.NoMetaInf => MTLogLevel.Error,
            LoadIssueType.InvalidClassMagicCode => MTLogLevel.Error,
            LoadIssueType.MissingClassSuper => MTLogLevel.Error,
            LoadIssueType.MissingClassField => MTLogLevel.Warning,
            LoadIssueType.LocalVariableIndexOutOfBounds => MTLogLevel.Error,
            LoadIssueType.MultiTypeLocalVariable => MTLogLevel.Info,
            LoadIssueType.BrokenFlow => MTLogLevel.Error,
            LoadIssueType.StackMismatch => MTLogLevel.Error,
            LoadIssueType.MissingVirtualAccess => MTLogLevel.Warning,
            LoadIssueType.QuestionableNativeCode => MTLogLevel.Info,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }

    public static uint GetSnapshotHash(this string s)
    {
        if (s.Length == 0)
            return 0;
        uint h = s[0];

        for (int i = 1; i < s.Length; i++)
        {
            uint uc = s[i];
            h ^= uc << ((i % 4) * 8);
        }

        return h;
    }
}