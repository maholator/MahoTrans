// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace MahoTrans.Abstractions;

/// <summary>
///     Capture of LCDUI clip state.
/// </summary>
public struct GraphicsClip
{
    /// <summary>
    ///     X coord of top-left area corner.
    /// </summary>
    public int X;

    /// <summary>
    ///     Y coord of top-left area corner.
    /// </summary>
    public int Y;

    /// <summary>
    ///     Width of the area.
    /// </summary>
    [JsonProperty(PropertyName = "W")] public int Width;

    /// <summary>
    ///     Height of the area.
    /// </summary>
    [JsonProperty(PropertyName = "H")] public int Height;

    /// <summary>
    ///     Initializes new instance.
    /// </summary>
    /// <param name="x">Area X.</param>
    /// <param name="y">Area Y.</param>
    /// <param name="width">Area width.</param>
    /// <param name="height">Area height.</param>
    public GraphicsClip(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}