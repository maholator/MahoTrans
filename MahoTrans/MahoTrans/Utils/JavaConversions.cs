using System.Text;

namespace MahoTrans.Utils;

public static class JavaConversions
{
    public static byte[] ToUnsigned(this sbyte[] arr) => (byte[])(Array)arr;
    public static sbyte[] ToSigned(this byte[] arr) => (sbyte[])(Array)arr;

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
}