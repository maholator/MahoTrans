// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.lcdui;
using MahoTrans;
using MahoTrans.Native;

namespace com.nokia.mid.ui;

public interface DirectGraphics : IJavaObject
{
    public void drawImage(Image img, int x, int y, int anchor, int m) => throw new AbstractCall();

    public void drawPixels(sbyte[] pixels, sbyte[] transparencyMask, int offset, int scanlength, int x, int y, int width,
        int height, int manipulation, int format) => throw new AbstractCall();

    public void drawPixels(int[] pixels, bool transparency, int offset, int scanlength, int x, int y, int width,
        int height, int manipulation, int format) => throw new AbstractCall();

    public void drawPixels(short[] pixels, bool transparency, int offset, int scanlength, int x, int y, int width,
        int height, int manipulation, int format) => throw new AbstractCall();

    public void drawPolygon(int[] xPoints, int xOffset, int[] yPoints, int yOffset, int nPoints, int argbColor) =>
        throw new AbstractCall();

    public void drawTriangle(int x1, int y1, int x2, int y2, int x3, int y3, int argbColor) => throw new AbstractCall();

    public void fillPolygon(int[] xPoints, int xOffset, int[] yPoints, int yOffset, int nPoints, int argbColor) =>
        throw new AbstractCall();

    public void fillTriangle(int x1, int y1, int x2, int y2, int x3, int y3, int argbColor) => throw new AbstractCall();

    public int getAlphaComponent() => throw new AbstractCall();

    public int getNativePixelFormat() => throw new AbstractCall();

    public void getPixels(sbyte[] pixels, sbyte[] transparencyMask, int offset, int scanlength, int x, int y, int width,
        int height, int format) => throw new AbstractCall();

    public void getPixels(int[] pixels, int offset, int scanlength, int x, int y, int width, int height, int format) =>
        throw new AbstractCall();

    public void getPixels(short[] pixels, int offset, int scanlength, int x, int y, int width, int height,
        int format) => throw new AbstractCall();

    public void setARGBColor(int argbColor) => throw new AbstractCall();

    public const int FLIP_HORIZONTAL = 8192;
    public const int FLIP_VERTICAL = 16384;
    public const int ROTATE_180 = 180;
    public const int ROTATE_270 = 270;
    public const int ROTATE_90 = 90;
    public const int TYPE_BYTE_1_GRAY = 1;
    public const int TYPE_BYTE_1_GRAY_VERTICAL = -1;
    public const int TYPE_BYTE_2_GRAY = 2;
    public const int TYPE_BYTE_332_RGB = 332;
    public const int TYPE_BYTE_4_GRAY = 4;
    public const int TYPE_BYTE_8_GRAY = 8;
    public const int TYPE_INT_888_RGB = 888;
    public const int TYPE_INT_8888_ARGB = 8888;
    public const int TYPE_USHORT_1555_ARGB = 1555;
    public const int TYPE_USHORT_444_RGB = 444;
    public const int TYPE_USHORT_4444_ARGB = 4444;
    public const int TYPE_USHORT_555_RGB = 555;
    public const int TYPE_USHORT_565_RGB = 565;
}
