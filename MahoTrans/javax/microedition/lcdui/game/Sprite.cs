// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using System;
using System.Reflection.Metadata;
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

    private Reference anImage265;
    private int anInt269;
    [JavaIgnore] private int[] anIntArray266 = null!;
    [JavaIgnore] private int[] anIntArray267 = null!;
    private int anInt270;
    private int anInt271;
    [JavaIgnore] private int[] anIntArray268 = null!;
    private int anInt416;
    private bool abool407;
    private int anInt272;
    private int anInt273;
    private int anInt274;
    private int anInt408;
    private int anInt409;
    private int anInt410;
    private int anInt411;
    private int anInt412;
    private int anInt413;
    private int anInt414;
    private int anInt415;

    [InitMethod]
    public void InitImage([JavaType(nameof(Image))] Reference r)
    {
        Image image = (Image)Jvm.ResolveObject(r);
        base.Init(image.getWidth(), image.getHeight());
    }

    [InitMethod]
    public void InitImage([JavaType(nameof(Image))] Reference r, int w, int h)
    {
        Image image = (Image)Jvm.ResolveObject(r);
        base.Init(w, h);
        if (w < 1 || h < 1 || image.getWidth() % w != 0 || image.getHeight() % h != 0)
            Jvm.Throw<IllegalArgumentException>();

    }

    [InitMethod]
    public void InitSprite([JavaType(nameof(Sprite))] Reference r)
    {
        if(r.IsNull)
            Jvm.Throw<NullPointerException>();
        Sprite sprite = (Sprite)Jvm.ResolveObject(r);
        base.Init(sprite.getWidth(), sprite.getHeight());

        anImage265 = Image.createImage(sprite.anImage265);
        anInt269 = sprite.anInt269;
        anIntArray266 = new int[anInt269];
        anIntArray267 = new int[anInt269];
        System.Array.Copy(sprite.anIntArray266, 0, anIntArray266, 0, sprite.getRawFrameCount());
        System.Array.Copy(sprite.anIntArray267, 0, anIntArray267, 0, sprite.getRawFrameCount());
        X = sprite.getX();
        Y = sprite.getY();
        anInt272 = sprite.anInt272;
        anInt273 = sprite.anInt273;
        anInt274 = sprite.anInt274;
        anInt408 = sprite.anInt408;
        anInt409 = sprite.anInt409;
        anInt410 = sprite.anInt410;
        anInt270 = sprite.anInt270;
        anInt271 = sprite.anInt271;
        method209(sprite.anInt411);
        setVisible(sprite.isVisible());
        anIntArray268 = new int[sprite.getFrameSequenceLength()];
        setFrameSequence1(sprite.anIntArray268);
        setFrame(sprite.getFrame());
        setRefPixelPosition(sprite.getRefPixelX(), sprite.getRefPixelY());
    }

    public void defineReferencePixel(int var1, int var2)
    {
        anInt272 = var1;
        anInt273 = var2;
    }

    public void setRefPixelPosition(int var1, int var2)
    {
        X = var1 - method206(anInt272, anInt273, anInt411);
        Y = var2 - method208(anInt272, anInt273, anInt411);
    }

    public int getRefPixelX()
    {
        return X + method206(anInt272, anInt273, anInt411);
    }

    public int getRefPixelY()
    {
        return Y + method208(anInt272, anInt273, anInt411);
    }

    public void setFrame(int var1)
    {
        if (var1 >= 0 && var1 < anIntArray268.Length)
        {
            anInt416 = var1;
        }
        else
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
    }

    public int getFrame()
    {
        return anInt416;
    }

    public int getRawFrameCount()
    {
        return anInt269;
    }

    public int getFrameSequenceLength()
    {
        return anIntArray268.Length;
    }

    public void nextFrame()
    {
        anInt416 = (anInt416 + 1) % anIntArray268.Length;
    }

    public void prevFrame()
    {
        Sprite var10000;
        int var10001;
        if (anInt416 == 0)
        {
            var10000 = this;
            var10001 = anIntArray268.Length;
        }
        else
        {
            var10000 = this;
            var10001 = anInt416;
        }

        var10000.anInt416 = var10001 - 1;
    }

    public new void paint([JavaType(nameof(Graphics))] Reference r)
    {
        if (r.IsNull)
            Jvm.Throw<NullPointerException>();
        Graphics g = (Graphics) Jvm.ResolveObject(r);
        if (Visible)
        {
            g.drawRegion(anImage265, anIntArray266[anIntArray268[anInt416]], anIntArray267[anIntArray268[anInt416]], anInt270, anInt271, anInt411, X, Y, 20);
        }
    }

    public void setFrameSequence([JavaType("[I")] Reference rvar1)
    {
        int[] var1 = Jvm.ResolveArray<int>(rvar1);
        setFrameSequence1(var1);
    }


    [JavaIgnore]
    public void setFrameSequence1(int[] var1)
    {
        int var2;
        if (var1 == null)
        {
            anInt416 = 0;
            abool407 = false;
            anIntArray268 = new int[anInt269];

            for (var2 = 0; var2 < anInt269; anIntArray268[var2] = var2++)
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
                if (var1[var2] < 0 || var1[var2] >= anInt269)
                {
                    Jvm.Throw<ArrayIndexOutOfBoundsException>();
                }
            }

            abool407 = true;
            anIntArray268 = new int[var1.Length];
            System.Array.Copy(var1, 0, anIntArray268, 0, var1.Length);
            anInt416 = 0;
        }
    }

    public void defineCollisionRectangle(int var1, int var2, int var3, int var4)
    {
        if (var3 >= 0 && var4 >= 0)
        {
            anInt274 = var1;
            anInt408 = var2;
            anInt409 = var3;
            anInt410 = var4;
            method209(anInt411);
        }
        else
        {
            Jvm.Throw<IllegalArgumentException>();
        }
    }

    public void setTransform(int var1)
    {
        method209(var1);
    }

    public bool collidesWith___sprite([JavaType(nameof(Sprite))] Reference rvar1, bool var2)
    {
        Sprite var1 = (Sprite)Jvm.ResolveObject(rvar1);
        if (var1.Visible && Visible)
        {
            int var3 = var1.X + var1.anInt412;
            int var4 = var1.Y + var1.anInt413;
            int var5 = var3 + var1.anInt414;
            int var6 = var4 + var1.anInt415;
            int var7 = X + this.anInt412;
            int var8 = Y + this.anInt413;
            int var9 = var7 + this.anInt414;
            int var10 = var8 + this.anInt415;
            if (method203(var3, var4, var5, var6, var7, var8, var9, var10))
            {
                if (var2)
                {
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

                    if (var1.anInt412 < 0)
                    {
                        var3 = var1.X;
                    }

                    if (var1.anInt413 < 0)
                    {
                        var4 = var1.Y;
                    }

                    if (var1.anInt412 + var1.anInt414 > var1.Width)
                    {
                        var5 = var1.X + var1.Width;
                    }

                    if (var1.anInt413 + var1.anInt415 > var1.Height)
                    {
                        var6 = var1.Y + var1.Height;
                    }

                    if (!method203(var3, var4, var5, var6, var7, var8, var9, var10))
                    {
                        return false;
                    }
                    else
                    {
                        int var11 = var7 < var3 ? var3 : var7;
                        int var12 = var8 < var4 ? var4 : var8;
                        int var13 = var9 < var5 ? var9 : var5;
                        int var14 = var10 < var6 ? var10 : var6;
                        int var15 = Math.abs(var13 - var11);
                        int var16 = Math.abs(var14 - var12);
                        int var17 = this.method205(var11, var12, var13, var14);
                        int var18 = this.method207(var11, var12, var13, var14);
                        int var19 = var1.method205(var11, var12, var13, var14);
                        int var20 = var1.method207(var11, var12, var13, var14);
                        return method204(var17, var18, var19, var20, this.anImage265, this.anInt411, var1.anImage265, var1.anInt411, var15, var16);
                    }
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public bool collidesWith___tiledLayer([JavaType(nameof(TiledLayer))] Reference rvar1, bool var2)
    {
        TiledLayer var1 = (TiledLayer)Jvm.ResolveObject(rvar1);
        if (var1.Visible && Visible)
        {
            int var3 = var1.X;
            int var4 = var1.Y;
            int var5 = var3 + var1.Width;
            int var6 = var4 + var1.Height;
            int var7 = var1.getCellWidth();
            int var8 = var1.getCellHeight();
            int var9 = X + this.anInt412;
            int var10 = Y + this.anInt413;
            int var11 = var9 + this.anInt414;
            int var12 = var10 + this.anInt415;
            int var13 = var1.getColumns();
            int var14 = var1.getRows();
            if (!method203(var3, var4, var5, var6, var9, var10, var11, var12))
            {
                return false;
            }
            else
            {
                int var15 = var9 <= var3 ? 0 : (var9 - var3) / var7;
                int var16 = var10 <= var4 ? 0 : (var10 - var4) / var8;
                int var17 = var11 < var5 ? (var11 - 1 - var3) / var7 : var13 - 1;
                int var18 = var12 < var6 ? (var12 - 1 - var4) / var8 : var14 - 1;
                int var19;
                int var20;
                if (!var2)
                {
                    for (var19 = var16; var19 <= var18; ++var19)
                    {
                        for (var20 = var15; var20 <= var17; ++var20)
                        {
                            if (var1.getCell(var20, var19) != 0)
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
                                if ((var25 = var1.getCell(var24, var21)) != 0)
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
                                    int var34 = var1.anIntArray266[var25] + (var26 - var22);
                                    int var35 = var1.anIntArray267[var25] + (var27 - var19);
                                    if (method204(var32, var33, var34, var35, this.anImage265, this.anInt411, var1.anImage265, 0, var30, var31))
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
        }
        else
        {
            return false;
        }
    }

    public bool collidesWith([JavaType(nameof(Image))] Reference var1, int var2, int var3, bool var4)
    {
        if (!Visible)
            return false;
        Image image = (Image)Jvm.ResolveObject(var1);
        int var7 = var2 + image.getWidth();
        int var8 = var3 + image.getHeight();
        int var9 = X + this.anInt412;
        int var10 = Y + this.anInt413;
        int var11 = var9 + this.anInt414;
        int var12 = var10 + this.anInt415;
        if (method203(var2, var3, var7, var8, var9, var10, var11, var12))
        {
            if (var4)
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
                    return method204(var19, var20, var21, var22, this.anImage265, this.anInt411, var1, 0, var17, var18);
                }
            }
            return true;
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
        this.anImage265 = var1;
        this.anInt270 = var2;
        this.anInt271 = var3;
        this.anInt269 = var7 * var8;
        this.anIntArray266 = new int[this.anInt269];
        this.anIntArray267 = new int[this.anInt269];
        if (!var4)
        {
            this.anInt416 = 0;
        }

        if (!this.abool407)
        {
            this.anIntArray268 = new int[this.anInt269];
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
        anInt274 = 0;
        anInt408 = 0;
        anInt409 = Width;
        anInt410 = Height;
    }

    private static bool method203(int var0, int var1, int var2, int var3, int var4, int var5, int var6, int var7)
    {
        return var4 < var2 && var5 < var3 && var6 > var0 && var7 > var1;
    }

    [JavaIgnore]
    private static bool method204(int var0, int var1, int var2, int var3, Reference rvar4, int var5, Reference rvar6, int var7, int var8, int var9)
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
        switch (anInt411)
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
                return var5 + anIntArray266[anIntArray268[anInt416]];
        }

        var5 = var10000 - var10001;
        return var5 + anIntArray266[anIntArray268[anInt416]];
    }

    private int method207(int var1, int var2, int var3, int var4)
    {
        int var5 = 0;
        int var10000;
        int var10001;
        switch (anInt411)
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
                return var5 + anIntArray267[anIntArray268[anInt416]];
        }

        var5 = var10000 - var10001;
        return var5 + anIntArray267[anIntArray268[anInt416]];
    }

    private void method209(int var1)
    {
        X = X + method206(anInt272, anInt273, anInt411) - method206(anInt272, anInt273, var1);
        Y = Y + method208(anInt272, anInt273, anInt411) - method208(anInt272, anInt273, var1);
        method210(var1);
        anInt411 = var1;
    }

    private void method210(int var1)
    {
        switch (var1)
        {
            case 0:
                anInt412 = anInt274;
                anInt413 = anInt408;
                anInt414 = anInt409;
                anInt415 = anInt410;
                Width = anInt270;
                Height = anInt271;
                return;
            case 1:
                anInt413 = anInt271 - (anInt408 + anInt410);
                anInt412 = anInt274;
                anInt414 = anInt409;
                anInt415 = anInt410;
                Width = anInt270;
                Height = anInt271;
                return;
            case 2:
                anInt412 = anInt270 - (anInt274 + anInt409);
                anInt413 = anInt408;
                anInt414 = anInt409;
                anInt415 = anInt410;
                Width = anInt270;
                Height = anInt271;
                return;
            case 3:
                anInt412 = anInt270 - (anInt409 + anInt274);
                anInt413 = anInt271 - (anInt410 + anInt408);
                anInt414 = anInt409;
                anInt415 = anInt410;
                Width = anInt270;
                Height = anInt271;
                return;
            case 4:
                anInt413 = anInt274;
                anInt412 = anInt408;
                anInt415 = anInt409;
                anInt414 = anInt410;
                Width = anInt271;
                Height = anInt270;
                return;
            case 5:
                anInt412 = anInt271 - (anInt410 + anInt408);
                anInt413 = anInt274;
                anInt415 = anInt409;
                anInt414 = anInt410;
                Width = anInt271;
                Height = anInt270;
                return;
            case 6:
                anInt412 = anInt408;
                anInt413 = anInt270 - (anInt409 + anInt274);
                anInt415 = anInt409;
                anInt414 = anInt410;
                Width = anInt271;
                Height = anInt270;
                return;
            case 7:
                anInt412 = anInt271 - (anInt410 + anInt408);
                anInt413 = anInt270 - (anInt409 + anInt274);
                anInt415 = anInt409;
                anInt414 = anInt410;
                Width = anInt271;
                Height = anInt270;
                return;
            default:
                Jvm.Throw<IllegalArgumentException>();
                return;
        }
    }

    public int method206(int var1, int var2, int var3)
    {
        bool var4 = false;
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
                var5 = anInt270 - var1 - 1;
                break;
            case 3:
                var5 = anInt270 - var1 - 1;
                break;
            case 4:
                var5 = var2;
                break;
            case 5:
                var5 = anInt271 - var2 - 1;
                break;
            case 6:
                var5 = var2;
                break;
            case 7:
                var5 = anInt271 - var2 - 1;
                break;
            default:
                Jvm.Throw<IllegalArgumentException>();
                return 0;
        }

        return var5;
    }

    public int method208(int var1, int var2, int var3)
    {
        bool var4 = false;
        int var5;
        switch (var3)
        {
            case 0:
                var5 = var2;
                break;
            case 1:
                var5 = anInt271 - var2 - 1;
                break;
            case 2:
                var5 = var2;
                break;
            case 3:
                var5 = anInt271 - var2 - 1;
                break;
            case 4:
                var5 = var1;
                break;
            case 5:
                var5 = var1;
                break;
            case 6:
                var5 = anInt270 - var1 - 1;
                break;
            case 7:
                var5 = anInt270 - var1 - 1;
                break;
            default:
                Jvm.Throw<IllegalArgumentException>();
                return 0;
        }

        return var5;
    }

}