// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using java.io;
using MahoTrans.Runtime;

namespace MahoTrans.Utils;

public static class JavaConversions
{
    /// <summary>
    ///     Takes sbyte[] as byte[]. This does not change actual data, runtime type and so on. No copy is made.
    /// </summary>
    /// <param name="arr">Array to cast.</param>
    /// <returns>sbyte[] taken as byte[].</returns>
    public static byte[] ToUnsigned(this sbyte[] arr) => (byte[])(Array)arr;

    /// <summary>
    ///     Takes byte[] as sbyte[]. This does not change actual data, runtime type and so on. No copy is made. MT and
    ///     NewtonsoftJson rely on runtime type of objects, but usage of this method doesn't change it. If resulting array
    ///     is going to be used inside JVM, <see cref="ConvertToSigned" /> must be used.
    /// </summary>
    /// <param name="arr">Array to cast.</param>
    /// <returns>byte[] taken as sbyte[].</returns>
    [Obsolete("Use ConvertToSigned instead. See xmldocs for details.", true)]
    public static sbyte[] ToSigned(this byte[] arr) => (sbyte[])(Array)arr;

    /// <summary>
    ///     Converts unsigned bytes to signed bytes. This creates a new array and actually casts each byte to sbyte.
    /// </summary>
    /// <param name="arr">Array to convert.</param>
    /// <returns>Converted array.</returns>
    public static sbyte[] ConvertToSigned(this byte[] arr)
    {
        return Array.ConvertAll(arr, x => (sbyte)x);
    }

    public static string DecodeJavaUnicode(this byte[] data)
    {
        List<char> chars = new();

        int i = 0;
        while (i < data.Length)
        {
            int currentByte = data[i++];
            if ((currentByte & 0x80) == 0)
            {
                chars.Add((char)(currentByte & 0x7F));
            }
            else if ((currentByte & 0xE0) == 0xC0)
            {
                chars.Add((char)(((currentByte & 0x1F) << 6) + (data[i++] & 0x3F)));
            }
            else
            {
                char c =
                    (char)
                    (((currentByte & 0xF) << 12)
                     + ((data[i++] & 0x3F) << 6)
                     + (data[i++] & 0x3F));
                chars.Add(c);
            }
        }

        return new string(chars.ToArray());
    }

    public static byte[] EncodeJavaUnicode(this string str)
    {
        List<byte> data = new List<byte>(str.Length);
        foreach (int c in str)
        {
            if (c >= 0x0001 && c <= 0x007F)
            {
                data.Add((byte)c);
            }
            else if (c == 0 || (c >= 0x0080 && c <= 0x07FF))
            {
                data.Add((byte)(0xc0 | (0x1f & (c >> 6))));
                data.Add((byte)(0x80 | (0x3f & c)));
            }
            else
            {
                data.Add((byte)(0xe0 | (0x0f & (c >> 12))));
                data.Add((byte)(0x80 | (0x3f & (c >> 6))));
                data.Add((byte)(0x80 | (0x3f & c)));
            }
        }

        return data.ToArray();
    }

    public static Encoding GetEncodingByName(this string name)
    {
        name = name.ToUpper();
        switch (name)
        {
            case "UTF8":
            case "UTF-8":
                return Encoding.UTF8;

            case "8859-1":
            case "ISO8859-1":
            case "ISO-8859-1":
            case "ISO-8859-1:1987":
            case "ISO-IR-100":
            case "LATIN1":
            case "CSISOLATIN1":
            case "CP819":
            case "IBM-819":
            case "IBM819":
            case "L1":
            case "ISO_8859_1":
                return Encoding.Latin1;

            case "IBM-1251":
            case "WINDOWS-1251":
            case "CP1251":
                return Encoding.GetEncoding("windows-1251");

            case "IBM-1252":
            case "WINDOWS-1252":
            case "CP1252":
                return Encoding.GetEncoding("windows-1252");

            case "646":
            case "ANSI_X3.4-1968":
            case "ANSI_X3.4-1986":
            case "CP367":
            case "ISO-646.IRV:1991":
            case "ISO646-US":
            case "US-ASCII":
            case "ASCII7":
            case "CSASCII":
            case "DEFAULT":
            case "DIRECT":
            case "IBM-367":
            case "ISO-646.IRV:1983":
            case "ISO-IR-6":
            case "US":
            case "ASCII":
                return Encoding.ASCII;

            default:
                JvmContext.Jvm!.Throw<UnsupportedEncodingException>();
                return null!;
        }
    }

    public static Reference AsJavaArray(this string[] list)
    {
        var jvm = JvmContext.Jvm!;
        Reference[] r = new Reference[list.Length];
        for (int i = 0; i < r.Length; i++)
        {
            r[i] = jvm.AllocateString(list[i]);
        }

        return jvm.WrapReferenceArray(r, "[Ljava/lang/String;");
    }
}
