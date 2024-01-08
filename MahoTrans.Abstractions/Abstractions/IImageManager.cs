// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Handles;

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit that manipulates images.
/// </summary>
public interface IImageManager : IToolkit
{
    /// <summary>
    ///     Creates mutable image.
    /// </summary>
    /// <param name="width">Width of image.</param>
    /// <param name="height">Height of image.</param>
    /// <returns>Image handle.</returns>
    ImageHandle CreateBuffer(int width, int height);

    /// <summary>
    ///     Creates immutable image from file.
    /// </summary>
    /// <param name="file">File content.</param>
    /// <returns>Image handle.</returns>
    ImageHandle CreateFromFile(Memory<byte> file);

    /// <summary>
    ///     Creates immutable image from RGB array.
    /// </summary>
    /// <param name="rgb">RGB array.</param>
    /// <param name="w">Width of image.</param>
    /// <param name="h">Height of image.</param>
    /// <param name="alpha">False to set all alpha to 255.</param>
    /// <returns>Image handle.</returns>
    ImageHandle CreateFromRgb(int[] rgb, int w, int h, bool alpha);

    /// <summary>
    ///     Creates immutable image from another image.
    /// </summary>
    /// <param name="image">Image to copy.</param>
    /// <returns>New image handle.</returns>
    ImageHandle CreateCopy(ImageHandle image);

    /// <summary>
    ///     Creates immutable image from part of another image.
    /// </summary>
    /// <param name="image">Image to copy.</param>
    /// <param name="x">Area X.</param>
    /// <param name="y">Area Y.</param>
    /// <param name="w">Area width.</param>
    /// <param name="h">Area height.</param>
    /// <param name="tr">Transform to apply.</param>
    /// <returns>New image handle.</returns>
    ImageHandle CreateCopy(ImageHandle image, int x, int y, int w, int h, SpriteTransform tr);

    /// <summary>
    ///     Gets image width.
    /// </summary>
    /// <param name="image">Image handle.</param>
    /// <returns>Width of image.</returns>
    int GetWidth(ImageHandle image);

    /// <summary>
    ///     Gets image height.
    /// </summary>
    /// <param name="image">Image handle.</param>
    /// <returns>Height of image.</returns>
    int GetHeight(ImageHandle image);

    /// <summary>
    ///     Determines if image is mutable.
    /// </summary>
    /// <param name="image">Image handle.</param>
    /// <returns>True if image is mutable.</returns>
    bool IsMutable(ImageHandle image);

    /// <summary>
    ///     Copies pixel data from part of the image.
    /// </summary>
    /// <param name="image">Image to copy.</param>
    /// <param name="target">Array to copy to.</param>
    /// <param name="offset">Offset in target array.</param>
    /// <param name="scanlength">See MIDP docs for explanation.</param>
    /// <param name="x">Area X.</param>
    /// <param name="y">Area Y.</param>
    /// <param name="w">Area width.</param>
    /// <param name="h">Area height.</param>
    void CopyRgb(ImageHandle image, int[] target, int offset, int scanlength, int x, int y, int w, int h);

    /// <summary>
    ///     Notifies toolkit that image is no longer needed.
    /// </summary>
    /// <param name="image">Image to release.</param>
    void ReleaseImage(ImageHandle image);

    /// <summary>
    ///     Creates a graphics object for specified buffer.
    /// </summary>
    /// <param name="image">Buffer to bind with. Must be mutable.</param>
    /// <returns>Handle of created object. Use <see cref="ResolveGraphics" /> to get it.</returns>
    GraphicsHandle GetGraphics(ImageHandle image);

    /// <summary>
    ///     Gets graphics object.
    /// </summary>
    /// <param name="handle">Handle of graphics object.</param>
    /// <returns>Graphics object.</returns>
    IGraphics ResolveGraphics(GraphicsHandle handle);

    /// <summary>
    ///     Notifies toolkit that graphics object is no longer needed.
    /// </summary>
    /// <param name="handle">Graphics object to release.</param>
    void ReleaseGraphics(GraphicsHandle handle);
}