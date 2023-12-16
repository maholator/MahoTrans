using System.Text;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Utils;
using Newtonsoft.Json;

namespace java.io;

public class InputStreamReader : Reader
{
    [JavaType(typeof(InputStream))] public Reference Stream;
    [JavaIgnore] [JsonProperty] private Decoder _decoder = null!;


    [InitMethod]
    public void Init([JavaType(typeof(InputStream))] Reference stream)
    {
        Stream = stream;
        _decoder = "UTF-8".GetEncodingByName().GetDecoder();
    }

    [InitMethod]
    public void Init([JavaType(typeof(InputStream))] Reference stream, [String] Reference enc)
    {
        Stream = stream;
        _decoder = (Jvm.ResolveStringOrDefault(enc) ?? "UTF-8").GetEncodingByName().GetDecoder();
    }

    public int decodeChar(int b)
    {
        // end of stream
        if (b == -1)
            return -1;

        Span<byte> arr = stackalloc byte[1];
        arr[0] = (byte)((uint)b & 0xFF);
        Span<char> result = stackalloc char[1];

        var count = _decoder.GetChars(arr, result, false);
        if (count == 1)
            return result[0];

        return -2;
    }
}