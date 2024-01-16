// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
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
    private int rawFrameCount;
    [JavaIgnore] private int[] anIntArray266 = null!;
    [JavaIgnore] private int[] anIntArray267 = null!;
    private int frameWidth;
    private int frameHeight;
    [JavaIgnore] private int[] anIntArray268 = null!;
    private int currentFrame;
    private bool abool407;
    private int refX;
    private int refY;
    private int collisionX;
    private int collisionY;
    private int collisionW;
    private int collisionH;
    private int transform;
    private int anInt412;
    private int anInt413;
    private int anInt414;
    private int anInt415;

    [InitMethod]
    public void InitImage([JavaType(nameof(lcdui.Image))] Reference r)
    {
        Image img = Jvm.Resolve<Image>(r);
        Image = r;
        base.Init(img.getWidth(), img.getHeight());
    }

    [InitMethod]
    public void InitImage([JavaType(nameof(lcdui.Image))] Reference r, int w, int h)
    {
        Image img = Jvm.Resolve<Image>(r);
        Image = r;
        base.Init(w, h);
        if (w < 1 || h < 1 || img.getWidth() % w != 0 || img.getHeight() % h != 0)
            Jvm.Throw<IllegalArgumentException>();
    }

    [InitMethod]
    public void InitSprite([JavaType(nameof(Sprite))] Reference r)
    {
        if (r.IsNull)
            Jvm.Throw<NullPointerException>();
        Sprite sprite = (Sprite)Jvm.ResolveObject(r);
        base.Init(sprite.getWidth(), sprite.getHeight());

        Image = lcdui.Image.createImage(sprite.Image);
        rawFrameCount = sprite.rawFrameCount;
        anIntArray266 = new int[rawFrameCount];
        anIntArray267 = new int[rawFrameCount];
        System.Array.Copy(sprite.anIntArray266, 0, anIntArray266, 0, sprite.getRawFrameCount());
        System.Array.Copy(sprite.anIntArray267, 0, anIntArray267, 0, sprite.getRawFrameCount());
        X = sprite.getX();
        Y = sprite.getY();
        refX = sprite.refX;
        refY = sprite.refY;
        collisionX = sprite.collisionX;
        collisionY = sprite.collisionY;
        collisionW = sprite.collisionW;
        collisionH = sprite.collisionH;
        frameWidth = sprite.frameWidth;
        frameHeight = sprite.frameHeight;
        setTransformInternal(sprite.transform);
        setVisible(sprite.isVisible());
        anIntArray268 = new int[sprite.getFrameSequenceLength()];
        setFrameSequenceInternal(sprite.anIntArray268);
        setFrame(sprite.getFrame());
        setRefPixelPosition(sprite.getRefPixelX(), sprite.getRefPixelY());
    }

    public void defineReferencePixel(int x, int y)
    {
        refX = x;
        refY = y;
    }

    public void setRefPixelPosition(int x, int y)
    {
        X = x - method206(refX, refY, transform);
        Y = y - method208(refX, refY, transform);
    }

    public int getRefPixelX()
    {
        return X + method206(refX, refY, transform);
    }

    public int getRefPixelY()
    {
        return Y + method208(refX, refY, transform);
    }

    public void setFrame(int frame)
    {
        if (frame < 0 || frame >= anIntArray268.Length)
            Jvm.Throw<IndexOutOfBoundsException>();

        currentFrame = frame;
    }

    public int getFrame()
    {
        return currentFrame;
    }

    public int getRawFrameCount()
    {
        return rawFrameCount;
    }

    public int getFrameSequenceLength()
    {
        return anIntArray268.Length;
    }

    public void nextFrame()
    {
        currentFrame = (currentFrame + 1) % anIntArray268.Length;
    }

    public void prevFrame()
    {
        if (currentFrame == 0)
        {
            currentFrame = anIntArray268.Length - 1;
            return;
        }

        currentFrame -= 1;
    }

    public new void paint([JavaType(nameof(Graphics))] Reference r)
    {
        Graphics g = Jvm.Resolve<Graphics>(r);
        if (Visible)
        {
            g.drawRegion(Image, anIntArray266[anIntArray268[currentFrame]], anIntArray267[anIntArray268[currentFrame]],
                frameWidth, frameHeight, transform, X, Y, 20);
        }
    }

    public void setFrameSequence([JavaType("[I")] Reference r)
    {
        int[]? a = Jvm.ResolveArrayOrDefault<int>(r);
        setFrameSequenceInternal(a);
    }


    [JavaIgnore]
    public void setFrameSequenceInternal(int[]? var1)
    {
        int var2;
        if (var1 == null)
        {
            currentFrame = 0;
            abool407 = false;
            anIntArray268 = new int[rawFrameCount];

            for (var2 = 0; var2 < rawFrameCount; anIntArray268[var2] = var2++)
            {
                ;
            }
        }
        else if (var1.Length < 1)
        {
            Jvm.Throw<IllegalArgumentException>();
        }
        else
        {
            for (var2 = 0; var2 < var1.Length; ++var2)
            {
                if (var1[var2] < 0 || var1[var2] >= rawFrameCount)
                {
                    Jvm.Throw<ArrayIndexOutOfBoundsException>();
                }
            }

            abool407 = true;
            anIntArray268 = new int[var1.Length];
            System.Array.Copy(var1, 0, anIntArray268, 0, var1.Length);
            currentFrame = 0;
        }
    }

    public void defineCollisionRectangle(int x, int y, int w, int h)
    {
        if (w < 0 || h < 0)
            Jvm.Throw<IllegalArgumentException>();
        collisionX = x;
        collisionY = y;
        collisionW = w;
        collisionH = h;
        setTransformInternal(transform);
    }

    public void setTransform(int t) => setTransformInternal(t);

    public bool collidesWith___sprite([JavaType(nameof(Sprite))] Reference r, bool pixelLevel)
    {
        Sprite another = (Sprite)Jvm.ResolveObject(r);
        if (!another.Visible || !Visible)
            return false;

        int var3 = another.X + another.anInt412;
        int var4 = another.Y + another.anInt413;
        int var5 = var3 + another.anInt414;
        int var6 = var4 + another.anInt415;
        int var7 = X + this.anInt412;
        int var8 = Y + this.anInt413;
        int var9 = var7 + this.anInt414;
        int var10 = var8 + this.anInt415;
        if (method203(var3, var4, var5, var6, var7, var8, var9, var10))
        {
            if (!pixelLevel)
                return true;

            if (this.anInt412 < 0)
            {
                var7 = X;
            }

            if (this.anInt413 < 0)
            {
                var8 = Y;
            }

            if (this.anInt412 + this.anInt414 > Width)
            {
                var9 = X + Width;
            }

            if (this.anInt413 + this.anInt415 > Height)
            {
                var10 = Y + Height;
            }

            if (another.anInt412 < 0)
            {
                var3 = another.X;
            }

            if (another.anInt413 < 0)
            {
                var4 = another.Y;
            }

            if (another.anInt412 + another.anInt414 > another.Width)
            {
                var5 = another.X + another.Width;
            }

            if (another.anInt413 + another.anInt415 > another.Height)
            {
                var6 = another.Y + another.Height;
            }

            if (!method203(var3, var4, var5, var6, var7, var8, var9, var10))
                return false;

            int var11 = var7 < var3 ? var3 : var7;
            int var12 = var8 < var4 ? var4 : var8;
            int var13 = var9 < var5 ? var9 : var5;
            int var14 = var10 < var6 ? var10 : var6;
            int var15 = Math.abs(var13 - var11);
            int var16 = Math.abs(var14 - var12);
            int var17 = this.method205(var11, var12, var13, var14);
            int var18 = this.method207(var11, var12, var13, var14);
            int var19 = another.method205(var11, var12, var13, var14);
            int var20 = another.method207(var11, var12, var13, var14);
            return imagesCollideByPixels(var17, var18, var19, var20, this.Image, this.transform, another.Image,
                another.transform, var15, var16);
        }
        else
        {
            return false;
        }
    }

    public bool collidesWith___tiledLayer([JavaType(nameof(TiledLayer))] Reference rvar1, bool pixelLevel)
    {
        var another = Jvm.Resolve<TiledLayer>(rvar1);
        if (!another.Visible || !Visible)
            return false;

        int var3 = another.X;
        int var4 = another.Y;
        int var5 = var3 + another.Width;
        int var6 = var4 + another.Height;
        int var7 = another.getCellWidth();
        int var8 = another.getCellHeight();
        int var9 = X + this.anInt412;
        int var10 = Y + this.anInt413;
        int var11 = var9 + this.anInt414;
        int var12 = var10 + this.anInt415;
        int var13 = another.getColumns();
        int var14 = another.getRows();

        if (!method203(var3, var4, var5, var6, var9, var10, var11, var12))
            return false;

        int var15 = var9 <= var3 ? 0 : (var9 - var3) / var7;
        int var16 = var10 <= var4 ? 0 : (var10 - var4) / var8;
        int var17 = var11 < var5 ? (var11 - 1 - var3) / var7 : var13 - 1;
        int var18 = var12 < var6 ? (var12 - 1 - var4) / var8 : var14 - 1;
        int var19;
        int var20;
        if (!pixelLevel)
        {
            for (var19 = var16; var19 <= var18; ++var19)
            {
                for (var20 = var15; var20 <= var17; ++var20)
                {
                    if (another.getCell(var20, var19) != 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        else
        {
            if (this.anInt412 < 0)
            {
                var9 = X;
            }

            if (this.anInt413 < 0)
            {
                var10 = Y;
            }

            if (this.anInt412 + this.anInt414 > Width)
            {
                var11 = X + Width;
            }

            if (this.anInt413 + this.anInt415 > Height)
            {
                var12 = Y + Height;
            }

            if (!method203(var3, var4, var5, var6, var9, var10, var11, var12))
            {
                return false;
            }
            else
            {
                var15 = var9 <= var3 ? 0 : (var9 - var3) / var7;
                var16 = var10 <= var4 ? 0 : (var10 - var4) / var8;
                var17 = var11 < var5 ? (var11 - 1 - var3) / var7 : var13 - 1;
                var18 = var12 < var6 ? (var12 - 1 - var4) / var8 : var14 - 1;
                var20 = (var19 = var16 * var8 + var4) + var8;

                for (int var21 = var16; var21 <= var18; var20 += var8)
                {
                    int var22;
                    int var23 = (var22 = var15 * var7 + var3) + var7;

                    for (int var24 = var15; var24 <= var17; var23 += var7)
                    {
                        int var25;
                        if ((var25 = another.getCell(var24, var21)) != 0)
                        {
                            int var26 = var9 < var22 ? var22 : var9;
                            int var27 = var10 < var19 ? var19 : var10;
                            int var28 = var11 < var23 ? var11 : var23;
                            int var29 = var12 < var20 ? var12 : var20;
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
                            int var32 = this.method205(var26, var27, var28, var29);
                            int var33 = this.method207(var26, var27, var28, var29);
                            int var34 = another.anIntArray266[var25] + (var26 - var22);
                            int var35 = another.anIntArray267[var25] + (var27 - var19);
                            if (imagesCollideByPixels(var32, var33, var34, var35, this.Image, this.transform,
                                    another.anImage265, 0, var30, var31))
                            {
                                return true;
                            }
                        }

                        ++var24;
                        var22 += var7;
                    }

                    ++var21;
                    var19 += var8;
                }

                return false;
            }
        }
    }

    public bool collidesWith([JavaType(nameof(lcdui.Image))] Reference r, int var2, int var3, bool pixelLevel)
    {
        if (!Visible)
            return false;
        Image another = Jvm.Resolve<Image>(r);
        int var7 = var2 + another.getWidth();
        int var8 = var3 + another.getHeight();
        int var9 = X + this.anInt412;
        int var10 = Y + this.anInt413;
        int var11 = var9 + this.anInt414;
        int var12 = var10 + this.anInt415;
        if (method203(var2, var3, var7, var8, var9, var10, var11, var12))
        {
            if (!pixelLevel)
                return true;
            if (this.anInt412 < 0)
            {
                var9 = X;
            }

            if (this.anInt413 < 0)
            {
                var10 = Y;
            }

            if (this.anInt412 + this.anInt414 > Width)
            {
                var11 = X + Width;
            }

            if (this.anInt413 + this.anInt415 > Height)
            {
                var12 = Y + Height;
            }

            if (!method203(var2, var3, var7, var8, var9, var10, var11, var12))
            {
                return false;
            }
            else
            {
                int var13 = var9 < var2 ? var2 : var9;
                int var14 = var10 < var3 ? var3 : var10;
                int var15 = var11 < var7 ? var11 : var7;
                int var16 = var12 < var8 ? var12 : var8;
                int var17 = Math.abs(var15 - var13);
                int var18 = Math.abs(var16 - var14);
                int var19 = this.method205(var13, var14, var15, var16);
                int var20 = this.method207(var13, var14, var15, var16);
                int var21 = var13 - var2;
                int var22 = var14 - var3;
                return imagesCollideByPixels(var19, var20, var21, var22, this.Image, this.transform, r, 0, var17, var18);
            }
        }

        return false;
    }

    [JavaIgnore]
    private void method201(Reference var1, int var2, int var3, bool var4)
    {
        Image image = (Image)Jvm.ResolveObject(var1);
        int var5 = image.getWidth();
        int var6 = image.getHeight();
        int var7 = var5 / var2;
        int var8 = var6 / var3;
        this.Image = var1;
        this.frameWidth = var2;
        this.frameHeight = var3;
        this.rawFrameCount = var7 * var8;
        this.anIntArray266 = new int[this.rawFrameCount];
        this.anIntArray267 = new int[this.rawFrameCount];
        if (!var4)
        {
            this.currentFrame = 0;
        }

        if (!this.abool407)
        {
            this.anIntArray268 = new int[this.rawFrameCount];
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
                    var10000 = var10 + var3;
                    break;
                }

                this.anIntArray266[var9] = var11;
                this.anIntArray267[var9] = var10;
                if (!this.abool407)
                {
                    this.anIntArray268[var9] = var9;
                }

                ++var9;
                var10000 = var11 + var2;
            }
        }
    }

    private void method202()
    {
        collisionX = 0;
        collisionY = 0;
        collisionW = Width;
        collisionH = Height;
    }

    private static bool method203(int var0, int var1, int var2, int var3, int var4, int var5, int var6, int var7)
    {
        return var4 < var2 && var5 < var3 && var6 > var0 && var7 > var1;
    }

    [JavaIgnore]
    private static bool imagesCollideByPixels(int var0, int var1, int var2, int var3, Reference rvar4, int var5, Reference rvar6,
        int var7, int var8, int var9)
    {
        Image var4 = (Image)Jvm.ResolveObject(rvar4);
        Image var6 = (Image)Jvm.ResolveObject(rvar6);

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

            var25 = var4;
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

            var25 = var4;
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

            var25 = var6;
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

            var25 = var6;
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
        switch (transform)
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
                return var5 + anIntArray266[anIntArray268[currentFrame]];
        }

        var5 = var10000 - var10001;
        return var5 + anIntArray266[anIntArray268[currentFrame]];
    }

    private int method207(int var1, int var2, int var3, int var4)
    {
        int var5 = 0;
        int var10000;
        int var10001;
        switch (transform)
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
                return var5 + anIntArray267[anIntArray268[currentFrame]];
        }

        var5 = var10000 - var10001;
        return var5 + anIntArray267[anIntArray268[currentFrame]];
    }

    private void setTransformInternal(int t)
    {
        X = X + method206(refX, refY, transform) - method206(refX, refY, t);
        Y = Y + method208(refX, refY, transform) - method208(refX, refY, t);
        method210(t);
        transform = t;
    }

    private void method210(int var1)
    {
        switch (var1)
        {
            case 0:
                anInt412 = collisionX;
                anInt413 = collisionY;
                anInt414 = collisionW;
                anInt415 = collisionH;
                Width = frameWidth;
                Height = frameHeight;
                return;
            case 1:
                anInt413 = frameHeight - (collisionY + collisionH);
                anInt412 = collisionX;
                anInt414 = collisionW;
                anInt415 = collisionH;
                Width = frameWidth;
                Height = frameHeight;
                return;
            case 2:
                anInt412 = frameWidth - (collisionX + collisionW);
                anInt413 = collisionY;
                anInt414 = collisionW;
                anInt415 = collisionH;
                Width = frameWidth;
                Height = frameHeight;
                return;
            case 3:
                anInt412 = frameWidth - (collisionW + collisionX);
                anInt413 = frameHeight - (collisionH + collisionY);
                anInt414 = collisionW;
                anInt415 = collisionH;
                Width = frameWidth;
                Height = frameHeight;
                return;
            case 4:
                anInt413 = collisionX;
                anInt412 = collisionY;
                anInt415 = collisionW;
                anInt414 = collisionH;
                Width = frameHeight;
                Height = frameWidth;
                return;
            case 5:
                anInt412 = frameHeight - (collisionH + collisionY);
                anInt413 = collisionX;
                anInt415 = collisionW;
                anInt414 = collisionH;
                Width = frameHeight;
                Height = frameWidth;
                return;
            case 6:
                anInt412 = collisionY;
                anInt413 = frameWidth - (collisionW + collisionX);
                anInt415 = collisionW;
                anInt414 = collisionH;
                Width = frameHeight;
                Height = frameWidth;
                return;
            case 7:
                anInt412 = frameHeight - (collisionH + collisionY);
                anInt413 = frameWidth - (collisionW + collisionX);
                anInt415 = collisionW;
                anInt414 = collisionH;
                Width = frameHeight;
                Height = frameWidth;
                return;
            default:
                Jvm.Throw<IllegalArgumentException>();
                return;
        }
    }

    public int method206(int var1, int var2, int var3)
    {
        int var5;
        switch (var3)
        {
            case 0:
                var5 = var1;
                break;
            case 1:
                var5 = var1;
                break;
            case 2:
                var5 = frameWidth - var1 - 1;
                break;
            case 3:
                var5 = frameWidth - var1 - 1;
                break;
            case 4:
                var5 = var2;
                break;
            case 5:
                var5 = frameHeight - var2 - 1;
                break;
            case 6:
                var5 = var2;
                break;
            case 7:
                var5 = frameHeight - var2 - 1;
                break;
            default:
                Jvm.Throw<IllegalArgumentException>();
                return 0;
        }

        return var5;
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
                var5 = frameHeight - var2 - 1;
                break;
            case 2:
                var5 = var2;
                break;
            case 3:
                var5 = frameHeight - var2 - 1;
                break;
            case 4:
                var5 = var1;
                break;
            case 5:
                var5 = var1;
                break;
            case 6:
                var5 = frameWidth - var1 - 1;
                break;
            case 7:
                var5 = frameWidth - var1 - 1;
                break;
            default:
                Jvm.Throw<IllegalArgumentException>();
                return 0;
        }

        return var5;
    }
}