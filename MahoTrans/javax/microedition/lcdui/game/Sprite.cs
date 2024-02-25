// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Abstractions;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Utils;
using Array = System.Array;
using Math = java.lang.Math;

namespace javax.microedition.lcdui.game;

public class Sprite : Layer
{
    public const int TRANS_NONE = 0;
    public const int TRANS_ROT90 = 5;
    public const int TRANS_ROT180 = 3;
    public const int TRANS_ROT270 = 6;
    public const int TRANS_MIRROR = 2;
    public const int TRANS_MIRROR_ROT90 = 7;
    public const int TRANS_MIRROR_ROT180 = 1;
    public const int TRANS_MIRROR_ROT270 = 4;

    public Reference Image;
    private int RawFrameCount;
    [JavaIgnore] private int[] FramesX = null!;
    [JavaIgnore] private int[] FramesY = null!;
    private int FrameWidth;
    private int FrameHeight;
    [JavaIgnore] private int[] FrameSequence = null!;
    private int CurrentFrame;
    private bool abool407;
    private int RefX;
    private int RefY;
    private int CollisionX;
    private int CollisionY;
    private int CollisionW;
    private int CollisionH;
    private int Transform;
    private int TransformedCollisionX;
    private int TransformedCollisionY;
    private int TransformedCollisionW;
    private int TransformedCollisionH;

    [InitMethod]
    public void InitImage([JavaType(typeof(Image))] Reference r)
    {
        Image image = Jvm.Resolve<Image>(r);
        Image = r;
        Init(image.getWidth(), image.getHeight());
        SetImage(image, image.getWidth(), image.getHeight(), false);
        InitCollision();
        setTransformInternal(0);
    }

    [InitMethod]
    public void InitImage([JavaType(typeof(Image))] Reference r, int w, int h)
    {
        Image image = Jvm.Resolve<Image>(r);
        Image = r;
        Init(w, h);
        if (w < 1 || h < 1 || image.getWidth() % w != 0 || image.getHeight() % h != 0)
            Jvm.Throw<IllegalArgumentException>();
        SetImage(image, image.getWidth(), image.getHeight(), false);
        InitCollision();
        setTransformInternal(0);
    }

    [InitMethod]
    public void InitSprite([JavaType(typeof(Sprite))] Reference r)
    {
        if (r.IsNull)
            Jvm.Throw<NullPointerException>();
        Sprite sprite = Jvm.Resolve<Sprite>(r);
        Init(sprite.getWidth(), sprite.getHeight());

        Image = lcdui.Image.createImage(sprite.Image);
        RawFrameCount = sprite.RawFrameCount;
        FramesX = new int[RawFrameCount];
        FramesY = new int[RawFrameCount];
        Array.Copy(sprite.FramesX, 0, FramesX, 0, sprite.getRawFrameCount());
        Array.Copy(sprite.FramesY, 0, FramesY, 0, sprite.getRawFrameCount());
        X = sprite.getX();
        Y = sprite.getY();
        RefX = sprite.RefX;
        RefY = sprite.RefY;
        CollisionX = sprite.CollisionX;
        CollisionY = sprite.CollisionY;
        CollisionW = sprite.CollisionW;
        CollisionH = sprite.CollisionH;
        FrameWidth = sprite.FrameWidth;
        FrameHeight = sprite.FrameHeight;
        setTransformInternal(sprite.Transform);
        setVisible(sprite.isVisible());
        FrameSequence = new int[sprite.getFrameSequenceLength()];
        setFrameSequenceInternal(sprite.FrameSequence);
        setFrame(sprite.getFrame());
        setRefPixelPosition(sprite.getRefPixelX(), sprite.getRefPixelY());
    }

    public void defineReferencePixel(int x, int y)
    {
        RefX = x;
        RefY = y;
    }

    public void setRefPixelPosition(int x, int y)
    {
        X = x - method206(RefX, RefY, Transform);
        Y = y - method208(RefX, RefY, Transform);
    }

    public int getRefPixelX()
    {
        return X + method206(RefX, RefY, Transform);
    }

    public int getRefPixelY()
    {
        return Y + method208(RefX, RefY, Transform);
    }

    public void setFrame(int sequenceIndex)
    {
        if (sequenceIndex < 0 || sequenceIndex >= FrameSequence.Length)
            Jvm.Throw<IndexOutOfBoundsException>();

        CurrentFrame = sequenceIndex;
    }

    public int getFrame() => CurrentFrame;

    public int getRawFrameCount() => RawFrameCount;

    public int getFrameSequenceLength() => FrameSequence.Length;

    public void nextFrame()
    {
        CurrentFrame = (CurrentFrame + 1) % FrameSequence.Length;
    }

    public void prevFrame()
    {
        if (CurrentFrame == 0)
        {
            CurrentFrame = FrameSequence.Length - 1;
            return;
        }

        CurrentFrame -= 1;
    }

    public new void paint([JavaType(typeof(Graphics))] Reference r)
    {
        Graphics g = Jvm.Resolve<Graphics>(r);
        if (Visible)
        {
            g.drawRegion(Image.As<Image>(), FramesX[FrameSequence[CurrentFrame]], FramesY[FrameSequence[CurrentFrame]],
                FrameWidth, FrameHeight, (SpriteTransform)Transform, X, Y, (GraphicsAnchor)20);
        }
    }

    public void setFrameSequence([JavaType("[I")] Reference r)
    {
        int[]? a = Jvm.ResolveArrayOrNull<int>(r);
        setFrameSequenceInternal(a);
    }

    public void setImage([JavaType(typeof(Image))] Reference r, int w, int h)
    {
        Image image = Jvm.Resolve<Image>(r);
        if (w >= 1 && h >= 1 && image.getWidth() % w == 0 && image.getHeight() % h == 0)
        {
            int var4 = image.getWidth() / w * (image.getHeight() / h);
            bool var5 = true;
            if (var4 < RawFrameCount)
            {
                var5 = false;
                abool407 = false;
            }

            Image = r;
            if (FrameWidth == w && FrameHeight == h)
            {
                SetImage(image, w, h, var5);
            }
            else
            {
                int var6 = X + method206(RefX, RefY, Transform);
                int var7 = Y + method208(RefX, RefY, Transform);
                Width = w;
                Height = h;
                SetImage(image, w, h, var5);
                InitCollision();
                X = var6 - method206(RefX, RefY, Transform);
                Y = var7 - method208(RefX, RefY, Transform);
                UpdateTransform(Transform);
            }
        }
        else
        {
            Jvm.Throw<IllegalArgumentException>();
        }
    }


    public void defineCollisionRectangle(int x, int y, int w, int h)
    {
        if (w < 0 || h < 0)
            Jvm.Throw<IllegalArgumentException>();
        CollisionX = x;
        CollisionY = y;
        CollisionW = w;
        CollisionH = h;
        setTransformInternal(Transform);
    }

    public void setTransform(int t) => setTransformInternal(t);

    public bool collidesWith___sprite([JavaType(typeof(Sprite))] Reference r, bool pixelLevel)
    {
        Sprite another = Jvm.Resolve<Sprite>(r);
        if (!another.Visible || !Visible)
            return false;

        int ax1 = another.X + another.TransformedCollisionX;
        int ay1 = another.Y + another.TransformedCollisionY;
        int ax2 = ax1 + another.TransformedCollisionW;
        int ay2 = ay1 + another.TransformedCollisionH;
        int bx1 = X + TransformedCollisionX;
        int by1 = Y + TransformedCollisionY;
        int bx2 = bx1 + TransformedCollisionW;
        int by2 = by1 + TransformedCollisionH;
        if (CheckCollision(ax1, ay1, ax2, ay2, bx1, by1, bx2, by2))
        {
            if (!pixelLevel)
                return true;

            if (TransformedCollisionX < 0)
            {
                bx1 = X;
            }

            if (TransformedCollisionY < 0)
            {
                by1 = Y;
            }

            if (TransformedCollisionX + TransformedCollisionW > Width)
            {
                bx2 = X + Width;
            }

            if (TransformedCollisionY + TransformedCollisionH > Height)
            {
                by2 = Y + Height;
            }

            if (another.TransformedCollisionX < 0)
            {
                ax1 = another.X;
            }

            if (another.TransformedCollisionY < 0)
            {
                ay1 = another.Y;
            }

            if (another.TransformedCollisionX + another.TransformedCollisionW > another.Width)
            {
                ax2 = another.X + another.Width;
            }

            if (another.TransformedCollisionY + another.TransformedCollisionH > another.Height)
            {
                ay2 = another.Y + another.Height;
            }

            if (!CheckCollision(ax1, ay1, ax2, ay2, bx1, by1, bx2, by2))
                return false;

            int var11 = bx1 < ax1 ? ax1 : bx1;
            int var12 = by1 < ay1 ? ay1 : by1;
            int var13 = bx2 < ax2 ? bx2 : ax2;
            int var14 = by2 < ay2 ? by2 : ay2;
            int var15 = Math.abs(var13 - var11);
            int var16 = Math.abs(var14 - var12);
            int var17 = method205(var11, var12, var13, var14);
            int var18 = method207(var11, var12, var13, var14);
            int var19 = another.method205(var11, var12, var13, var14);
            int var20 = another.method207(var11, var12, var13, var14);
            return imagesCollideByPixels(var17, var18, var19, var20, Image, Transform, another.Image,
                another.Transform, var15, var16);
        }

        return false;
    }

    public bool collidesWith___tiledLayer([JavaType(typeof(TiledLayer))] Reference r, bool pixelLevel)
    {
        var another = Jvm.Resolve<TiledLayer>(r);
        if (!another.Visible || !Visible)
            return false;

        int ax1 = another.X;
        int ay1 = another.Y;
        int ax2 = ax1 + another.Width;
        int ay2 = ay1 + another.Height;
        int cw = another.getCellWidth();
        int ch = another.getCellHeight();
        int bx1 = X + TransformedCollisionX;
        int by1 = Y + TransformedCollisionY;
        int bx2 = bx1 + TransformedCollisionW;
        int by2 = by1 + TransformedCollisionH;
        int cols = another.getColumns();
        int rows = another.getRows();

        if (!CheckCollision(ax1, ay1, ax2, ay2, bx1, by1, bx2, by2))
            return false;

        int var15 = bx1 <= ax1 ? 0 : (bx1 - ax1) / cw;
        int var16 = by1 <= ay1 ? 0 : (by1 - ay1) / ch;
        int var17 = bx2 < ax2 ? (bx2 - 1 - ax1) / cw : cols - 1;
        int var18 = by2 < ay2 ? (by2 - 1 - ay1) / ch : rows - 1;
        int cy;
        int cx;
        if (!pixelLevel)
        {
            for (cy = var16; cy <= var18; ++cy)
            {
                for (cx = var15; cx <= var17; ++cx)
                {
                    if (another.getCell(cx, cy) != 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        if (TransformedCollisionX < 0)
        {
            bx1 = X;
        }

        if (TransformedCollisionY < 0)
        {
            by1 = Y;
        }

        if (TransformedCollisionX + TransformedCollisionW > Width)
        {
            bx2 = X + Width;
        }

        if (TransformedCollisionY + TransformedCollisionH > Height)
        {
            by2 = Y + Height;
        }

        if (!CheckCollision(ax1, ay1, ax2, ay2, bx1, by1, bx2, by2))
        {
            return false;
        }

        var15 = bx1 <= ax1 ? 0 : (bx1 - ax1) / cw;
        var16 = by1 <= ay1 ? 0 : (by1 - ay1) / ch;
        var17 = bx2 < ax2 ? (bx2 - 1 - ax1) / cw : cols - 1;
        var18 = by2 < ay2 ? (by2 - 1 - ay1) / ch : rows - 1;
        cx = (cy = var16 * ch + ay1) + ch;

        for (int var21 = var16; var21 <= var18; cx += ch)
        {
            int var22;
            int var23 = (var22 = var15 * cw + ax1) + cw;

            for (int var24 = var15; var24 <= var17; var23 += cw)
            {
                int var25;
                if ((var25 = another.getCell(var24, var21)) != 0)
                {
                    int var26 = bx1 < var22 ? var22 : bx1;
                    int var27 = by1 < cy ? cy : by1;
                    int var28 = bx2 < var23 ? bx2 : var23;
                    int var29 = by2 < cx ? by2 : cx;
                    int var30;
                    if (var26 > var28)
                    {
                        var30 = var28;
                        var28 = var26;
                        var26 = var30;
                    }

                    if (var27 > var29)
                    {
                        var30 = var29;
                        var29 = var27;
                        var27 = var30;
                    }

                    var30 = var28 - var26;
                    int var31 = var29 - var27;
                    int var32 = method205(var26, var27, var28, var29);
                    int var33 = method207(var26, var27, var28, var29);
                    int var34 = another.CellsX[var25] + (var26 - var22);
                    int var35 = another.CellsY[var25] + (var27 - cy);
                    if (imagesCollideByPixels(var32, var33, var34, var35, Image, Transform,
                            another.Image, 0, var30, var31))
                    {
                        return true;
                    }
                }

                ++var24;
                var22 += cw;
            }

            ++var21;
            cy += ch;
        }

        return false;
    }

    public bool collidesWith___image([JavaType(typeof(Image))] Reference r, int x, int y, bool pixelLevel)
    {
        if (!Visible)
            return false;
        Image another = Jvm.Resolve<Image>(r);
        int x2 = x + another.getWidth();
        int y2 = y + another.getHeight();
        int bx1 = X + TransformedCollisionX;
        int by1 = Y + TransformedCollisionY;
        int bx2 = bx1 + TransformedCollisionW;
        int by2 = by1 + TransformedCollisionH;
        if (CheckCollision(x, y, x2, y2, bx1, by1, bx2, by2))
        {
            if (!pixelLevel)
                return true;
            if (TransformedCollisionX < 0)
            {
                bx1 = X;
            }

            if (TransformedCollisionY < 0)
            {
                by1 = Y;
            }

            if (TransformedCollisionX + TransformedCollisionW > Width)
            {
                bx2 = X + Width;
            }

            if (TransformedCollisionY + TransformedCollisionH > Height)
            {
                by2 = Y + Height;
            }

            if (!CheckCollision(x, y, x2, y2, bx1, by1, bx2, by2))
            {
                return false;
            }

            int var13 = bx1 < x ? x : bx1;
            int var14 = by1 < y ? y : by1;
            int var15 = bx2 < x2 ? bx2 : x2;
            int var16 = by2 < y2 ? by2 : y2;
            int var17 = Math.abs(var15 - var13);
            int var18 = Math.abs(var16 - var14);
            int var19 = method205(var13, var14, var15, var16);
            int var20 = method207(var13, var14, var15, var16);
            int var21 = var13 - x;
            int var22 = var14 - y;
            return imagesCollideByPixels(var19, var20, var21, var22, Image, Transform, r, 0, var17, var18);
        }

        return false;
    }

    [JavaIgnore]
    private void setFrameSequenceInternal(int[]? sequence)
    {
        if (sequence == null)
        {
            CurrentFrame = 0;
            abool407 = false;
            FrameSequence = new int[RawFrameCount];

            for (int i = 0; i < RawFrameCount; FrameSequence[i] = i++) ;
        }
        else if (sequence.Length < 1)
        {
            Jvm.Throw<IllegalArgumentException>();
        }
        else
        {
            for (int i = 0; i < sequence.Length; ++i)
            {
                if (sequence[i] < 0 || sequence[i] >= RawFrameCount)
                {
                    Jvm.Throw<ArrayIndexOutOfBoundsException>();
                }
            }

            abool407 = true;
            FrameSequence = new int[sequence.Length];
            Array.Copy(sequence, 0, FrameSequence, 0, sequence.Length);
            CurrentFrame = 0;
        }
    }

    [JavaIgnore]
    private void SetImage(Image image, int w, int h, bool var4)
    {
        int var5 = image.getWidth();
        int var6 = image.getHeight();
        int var7 = var5 / w;
        int var8 = var6 / h;
        FrameWidth = w;
        FrameHeight = h;
        RawFrameCount = var7 * var8;
        FramesX = new int[RawFrameCount];
        FramesY = new int[RawFrameCount];
        if (!var4)
        {
            CurrentFrame = 0;
        }

        if (!abool407)
        {
            FrameSequence = new int[RawFrameCount];
        }

        int var9 = 0;
        int var10000 = 0;

        while (true)
        {
            int var10 = var10000;
            if (var10000 >= var6)
            {
                return;
            }

            var10000 = 0;

            while (true)
            {
                int var11 = var10000;
                if (var10000 >= var5)
                {
                    var10000 = var10 + h;
                    break;
                }

                FramesX[var9] = var11;
                FramesY[var9] = var10;
                if (!abool407)
                {
                    FrameSequence[var9] = var9;
                }

                ++var9;
                var10000 = var11 + w;
            }
        }
    }

    private void InitCollision()
    {
        CollisionX = 0;
        CollisionY = 0;
        CollisionW = Width;
        CollisionH = Height;
    }

    private static bool CheckCollision(int var0, int var1, int var2, int var3, int var4, int var5, int var6, int var7)
    {
        return var4 < var2 && var5 < var3 && var6 > var0 && var7 > var1;
    }

    [JavaIgnore]
    private static bool imagesCollideByPixels(int var0, int var1, int var2, int var3, Reference r1, int var5,
        Reference r2,
        int var7, int var8, int var9)
    {
        Image img1 = Jvm.Resolve<Image>(r1);
        Image img2 = Jvm.Resolve<Image>(r2);

        int var10;
        int[] var11 = new int[var10 = var9 * var8];
        int[] var12 = new int[var10];
        int var13;
        int var14;
        int var15;
        int var10000;
        int[] var10001;
        byte var10002;
        int var10003;
        int var10004;
        int var10005;
        int var10006;
        int var10007;
        Image var25;
        if (0 != (var5 & 4))
        {
            if (0 != (var5 & 1))
            {
                var13 = -var9;
                var10000 = var10 - var9;
            }
            else
            {
                var13 = var9;
                var10000 = 0;
            }

            var14 = var10000;
            if (0 != (var5 & 2))
            {
                var15 = -1;
                var14 += var9 - 1;
            }
            else
            {
                var15 = 1;
            }

            var25 = img1;
            var10001 = var11;
            var10002 = 0;
            var10003 = var9;
            var10004 = var0;
            var10005 = var1;
            var10006 = var9;
            var10007 = var8;
        }
        else
        {
            if (0 != (var5 & 1))
            {
                var14 = var10 - var8;
                var10000 = -var8;
            }
            else
            {
                var14 = 0;
                var10000 = var8;
            }

            var15 = var10000;
            if (0 != (var5 & 2))
            {
                var13 = -1;
                var14 += var8 - 1;
            }
            else
            {
                var13 = 1;
            }

            var25 = img1;
            var10001 = var11;
            var10002 = 0;
            var10003 = var8;
            var10004 = var0;
            var10005 = var1;
            var10006 = var8;
            var10007 = var9;
        }

        Toolkit.Images.CopyRgb(var25.Handle, var10001, var10002, var10003, var10004, var10005, var10006, var10007);
        int var16;
        int var17;
        int var18;
        if (0 != (var7 & 4))
        {
            if (0 != (var7 & 1))
            {
                var16 = -var9;
                var10000 = var10 - var9;
            }
            else
            {
                var16 = var9;
                var10000 = 0;
            }

            var17 = var10000;
            if (0 != (var7 & 2))
            {
                var18 = -1;
                var17 += var9 - 1;
            }
            else
            {
                var18 = 1;
            }

            var25 = img2;
            var10001 = var12;
            var10002 = 0;
            var10003 = var9;
            var10004 = var2;
            var10005 = var3;
            var10006 = var9;
            var10007 = var8;
        }
        else
        {
            if (0 != (var7 & 1))
            {
                var17 = var10 - var8;
                var10000 = -var8;
            }
            else
            {
                var17 = 0;
                var10000 = var8;
            }

            var18 = var10000;
            if (0 != (var7 & 2))
            {
                var16 = -1;
                var17 += var8 - 1;
            }
            else
            {
                var16 = 1;
            }

            var25 = img2;
            var10001 = var12;
            var10002 = 0;
            var10003 = var8;
            var10004 = var2;
            var10005 = var3;
            var10006 = var8;
            var10007 = var9;
        }

        Toolkit.Images.CopyRgb(var25.Handle, var10001, var10002, var10003, var10004, var10005, var10006, var10007);
        int var19 = 0;
        int var20 = var14;

        for (int var21 = var17; var19 < var9; ++var19)
        {
            int var22 = 0;
            int var23 = var20;

            for (int var24 = var21; var22 < var8; ++var22)
            {
                if ((var11[var23] & -16777216) != 0 && (var12[var24] & -16777216) != 0)
                {
                    return true;
                }

                var23 += var13;
                var24 += var16;
            }

            var20 += var15;
            var21 += var18;
        }

        return false;
    }

    private int method205(int var1, int var2, int var3, int var4)
    {
        int var5 = 0;
        int var10000;
        int var10001;
        switch (Transform)
        {
            case 0:
            case 1:
                var10000 = var1;
                var10001 = X;
                break;
            case 2:
            case 3:
                var10000 = X + Width;
                var10001 = var3;
                break;
            case 4:
            case 5:
                var10000 = var2;
                var10001 = Y;
                break;
            case 6:
            case 7:
                var10000 = Y + Height;
                var10001 = var4;
                break;
            default:
                return var5 + FramesX[FrameSequence[CurrentFrame]];
        }

        var5 = var10000 - var10001;
        return var5 + FramesX[FrameSequence[CurrentFrame]];
    }

    private int method207(int var1, int var2, int var3, int var4)
    {
        int var5 = 0;
        int var10000;
        int var10001;
        switch (Transform)
        {
            case 0:
            case 2:
                var10000 = var2;
                var10001 = Y;
                break;
            case 1:
            case 3:
                var10000 = Y + Height;
                var10001 = var4;
                break;
            case 4:
            case 6:
                var10000 = var1;
                var10001 = X;
                break;
            case 5:
            case 7:
                var10000 = X + Width;
                var10001 = var3;
                break;
            default:
                return var5 + FramesY[FrameSequence[CurrentFrame]];
        }

        var5 = var10000 - var10001;
        return var5 + FramesY[FrameSequence[CurrentFrame]];
    }

    private void setTransformInternal(int t)
    {
        X = X + method206(RefX, RefY, Transform) - method206(RefX, RefY, t);
        Y = Y + method208(RefX, RefY, Transform) - method208(RefX, RefY, t);
        UpdateTransform(t);
        Transform = t;
    }

    private void UpdateTransform(int transform)
    {
        switch (transform)
        {
            case 0:
                TransformedCollisionX = CollisionX;
                TransformedCollisionY = CollisionY;
                TransformedCollisionW = CollisionW;
                TransformedCollisionH = CollisionH;
                Width = FrameWidth;
                Height = FrameHeight;
                return;
            case 1:
                TransformedCollisionY = FrameHeight - (CollisionY + CollisionH);
                TransformedCollisionX = CollisionX;
                TransformedCollisionW = CollisionW;
                TransformedCollisionH = CollisionH;
                Width = FrameWidth;
                Height = FrameHeight;
                return;
            case 2:
                TransformedCollisionX = FrameWidth - (CollisionX + CollisionW);
                TransformedCollisionY = CollisionY;
                TransformedCollisionW = CollisionW;
                TransformedCollisionH = CollisionH;
                Width = FrameWidth;
                Height = FrameHeight;
                return;
            case 3:
                TransformedCollisionX = FrameWidth - (CollisionW + CollisionX);
                TransformedCollisionY = FrameHeight - (CollisionH + CollisionY);
                TransformedCollisionW = CollisionW;
                TransformedCollisionH = CollisionH;
                Width = FrameWidth;
                Height = FrameHeight;
                return;
            case 4:
                TransformedCollisionY = CollisionX;
                TransformedCollisionX = CollisionY;
                TransformedCollisionH = CollisionW;
                TransformedCollisionW = CollisionH;
                Width = FrameHeight;
                Height = FrameWidth;
                return;
            case 5:
                TransformedCollisionX = FrameHeight - (CollisionH + CollisionY);
                TransformedCollisionY = CollisionX;
                TransformedCollisionH = CollisionW;
                TransformedCollisionW = CollisionH;
                Width = FrameHeight;
                Height = FrameWidth;
                return;
            case 6:
                TransformedCollisionX = CollisionY;
                TransformedCollisionY = FrameWidth - (CollisionW + CollisionX);
                TransformedCollisionH = CollisionW;
                TransformedCollisionW = CollisionH;
                Width = FrameHeight;
                Height = FrameWidth;
                return;
            case 7:
                TransformedCollisionX = FrameHeight - (CollisionH + CollisionY);
                TransformedCollisionY = FrameWidth - (CollisionW + CollisionX);
                TransformedCollisionH = CollisionW;
                TransformedCollisionW = CollisionH;
                Width = FrameHeight;
                Height = FrameWidth;
                return;
            default:
                Jvm.Throw<IllegalArgumentException>();
                return;
        }
    }

    public int method206(int var1, int var2, int transform)
    {
        int r;
        switch (transform)
        {
            case 0:
                r = var1;
                break;
            case 1:
                r = var1;
                break;
            case 2:
                r = FrameWidth - var1 - 1;
                break;
            case 3:
                r = FrameWidth - var1 - 1;
                break;
            case 4:
                r = var2;
                break;
            case 5:
                r = FrameHeight - var2 - 1;
                break;
            case 6:
                r = var2;
                break;
            case 7:
                r = FrameHeight - var2 - 1;
                break;
            default:
                Jvm.Throw<IllegalArgumentException>();
                return 0;
        }

        return r;
    }

    public int method208(int var1, int var2, int var3)
    {
        int var5;
        switch (var3)
        {
            case 0:
                var5 = var2;
                break;
            case 1:
                var5 = FrameHeight - var2 - 1;
                break;
            case 2:
                var5 = var2;
                break;
            case 3:
                var5 = FrameHeight - var2 - 1;
                break;
            case 4:
                var5 = var1;
                break;
            case 5:
                var5 = var1;
                break;
            case 6:
                var5 = FrameWidth - var1 - 1;
                break;
            case 7:
                var5 = FrameWidth - var1 - 1;
                break;
            default:
                Jvm.Throw<IllegalArgumentException>();
                return 0;
        }

        return var5;
    }
}