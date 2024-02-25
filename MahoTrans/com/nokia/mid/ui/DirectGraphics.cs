// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;

namespace com.nokia.mid.ui;

[JavaInterface]
public interface DirectGraphics
{
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