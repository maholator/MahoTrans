// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
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

    public static string Join<T>(this IEnumerable<T> sequence, string separator = "")
    {
        return string.Join(separator, sequence);
    }

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
        var ba = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(ba);
    }

    public static string AsBase64(this string str)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
    }

    public static string DecodeBase64(this string str)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(str));
    }

    public static LogLevel GetSeverity(this LoadIssueType type)
    {
        return type switch
        {
            LoadIssueType.MissingClassAccess => LogLevel.Warning,
            LoadIssueType.MissingMethodAccess => LogLevel.Error,
            LoadIssueType.MissingFieldAccess => LogLevel.Error,
            LoadIssueType.InvalidConstant => LogLevel.Error,
            LoadIssueType.NoMetaInf => LogLevel.Error,
            LoadIssueType.InvalidClassMagicCode => LogLevel.Error,
            LoadIssueType.MissingClassSuper => LogLevel.Error,
            LoadIssueType.MissingClassField => LogLevel.Warning,
            LoadIssueType.LocalVariableIndexOutOfBounds => LogLevel.Error,
            LoadIssueType.MultiTypeLocalVariable => LogLevel.Info,
            LoadIssueType.MethodWithoutReturn => LogLevel.Error,
            _ => 0
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