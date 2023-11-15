using System.Text;
using MahoTrans.Runtime;

namespace MahoTrans.Utils;

public static class JavaConversions
{
    public static byte[] ToUnsigned(this sbyte[] arr) => (byte[])(Array)arr;

    /// <summary>
    /// Force-casts byte[] to sbyte[]. This does not change actual data. Consider using <see cref="ConvertToSigned"/> instead.
    /// </summary>
    /// <param name="arr">Array to cast.</param>
    /// <returns>byte[] taken as sbyte[].</returns>
    public static sbyte[] ToSigned(this byte[] arr) => (sbyte[])(Array)arr;

    /// <summary>
    /// Converts unsigned bytes to signed bytes. This creates a new array and actually casts each byte to sbyte.
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

    public static string DecodeDefault(this byte[] data)
    {
        // TODO iso8859-1
        return data.DecodeUTF8();
    }

    public static byte[] EncodeDefault(this string data)
    {
        // TODO iso8859-1
        return data.EncodeUTF8();
    }

    public static string DecodeUTF8(this byte[] data) => Encoding.UTF8.GetString(data);

    public static byte[] EncodeUTF8(this string data) => Encoding.UTF8.GetBytes(data);

    public static Reference ToHeap(this string[] list, JvmState heap)
    {
        Reference[] r = new Reference[list.Length];
        for (int i = 0; i < r.Length; i++)
        {
            r[i] = heap.AllocateString(list[i]);
        }

        return heap.AllocateArray(r, "[java/lang/String");
    }
}