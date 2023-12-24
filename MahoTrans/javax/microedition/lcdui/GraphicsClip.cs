using Newtonsoft.Json;

namespace javax.microedition.lcdui;

public struct GraphicsClip
{
    public int X;
    public int Y;
    [JsonProperty(PropertyName = "W")] public int Width;
    [JsonProperty(PropertyName = "H")] public int Height;

    public GraphicsClip(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}