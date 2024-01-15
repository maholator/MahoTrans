// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using static System.Net.Mime.MediaTypeNames;
using System;

namespace javax.microedition.lcdui.game;

public class TiledLayer : Layer
{
    private int anInt269;
    private int anInt270;
    private int anInt271;
    private int anInt272;
    [JavaIgnore] private int[][] anIntArrayArray264 = null!;
    public Reference anImage265;
    private int anInt273;
    [JavaIgnore] public int[] anIntArray266 = null!;
    [JavaIgnore] public int[] anIntArray267 = null!;
    [JavaIgnore] private int[]? anIntArray268 = null!;
    private int anInt274;

    [InitMethod]
    public void Init(int anInt272, int anInt273, [JavaType(nameof(Image))] Reference r, int n, int n2)
    {
        Image image = (Image)Jvm.ResolveObject(r);
        if (image.getWidth() % n != 0 || image.getHeight() % n2 != 0)
            Jvm.Throw<IllegalArgumentException>();

        anInt272 = anInt272;
        anInt271 = anInt273;
        anIntArrayArray264 = new int[anInt273][];
        for(int i = 0; i < anInt273; i++)
        {
            anIntArrayArray264[i] = new int[anInt272];
        }
        method111(image, image.getWidth() / n * (image.getHeight() / n2) + 1, n, n2, true);
    }

    public int createAnimatedTile(int n)
    {
        if (n < 0 || n >= anInt273)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        if (anIntArray268 == null)
        {
            anIntArray268 = new int[4];
            anInt274 = 1;
        }
        else if (anInt274 == anIntArray268.Length)
        {
            int[] anIntArray268 = new int[this.anIntArray268.Length * 2];
            System.Array.Copy(this.anIntArray268, 0, anIntArray268, 0, this.anIntArray268.Length);
            this.anIntArray268 = anIntArray268;
        }
        anIntArray268[anInt274] = n;
        ++anInt274;
        return -(anInt274 - 1);
    }

    public void setAnimatedTile(int n, int n2)
    {
        if (n2 < 0 || n2 >= anInt273)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        n = -n;
        if (anIntArray268 == null || n <= 0 || n >= anInt274)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        anIntArray268[n] = n2;
    }

    public int getAnimatedTile(int n)
    {
        n = -n;
        if (anIntArray268 == null || n <= 0 || n >= anInt274)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        return anIntArray268[n];
    }

    public void setCell(int n, int n2, int n3)
    {
        if (n < 0 || n >= anInt272 || n2 < 0 || n2 >= anInt271)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        if (n3 > 0)
        {
            if (n3 >= anInt273)
            {
                Jvm.Throw<IndexOutOfBoundsException>();
            }
        }
        else if (n3 < 0 && (anIntArray268 == null || -n3 >= anInt274))
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        anIntArrayArray264[n2][n] = n3;
    }

    public int getCell(int n, int n2)
    {
        if (n < 0 || n >= anInt272 || n2 < 0 || n2 >= anInt271)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        return anIntArrayArray264[n2][n];
    }



    public void fillCells(int n, int n2, int n3, int n4, int n5)
    {
        if (n < 0 || n >= this.anInt272 || n2 < 0 || n2 >= this.anInt271 || n3 < 0 || n + n3 > this.anInt272 || n4 < 0 || n2 + n4 > this.anInt271)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        if (n5 > 0)
        {
            if (n5 >= this.anInt273)
            {
                Jvm.Throw<IndexOutOfBoundsException>();
            }
        }
        else if (n5 < 0 && (this.anIntArray268 == null || -n5 >= this.anInt274))
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        for (int i = n2; i < n2 + n4; ++i)
        {
            for (int j = n; j < n + n3; ++j)
            {
                this.anIntArrayArray264[i][j] = n5;
            }
        }
    }

    public int getCellWidth()
    {
        return this.anInt270;
    }

    public int getCellHeight()
    {
        return this.anInt269;
    }

    public int getColumns()
    {
        return this.anInt272;
    }

    public int getRows()
    {
        return this.anInt271;
    }

    public void setStaticTileSet([JavaType(nameof(Image))] Reference r, int n, int n2)
    {
        Image image = (Image)Jvm.ResolveObject(r);
        if (n < 1 || n2 < 1 || image.getWidth() % n != 0 || image.getHeight() % n2 != 0)
        {
            Jvm.Throw<IllegalArgumentException>();
        }
        Width = this.anInt272 * n;
        Height = this.anInt271 * n2;
        int n3;
        int n4;
        int n5;
        int n6;
        bool b;
        if ((n3 = image.getWidth() / n * (image.getHeight() / n2)) >= this.anInt273 - 1)
        {
            n4 = n3 + 1;
            n5 = n;
            n6 = n2;
            b = true;
        }
        else
        {
            n4 = n3 + 1;
            n5 = n;
            n6 = n2;
            b = false;
        }
        method111(r, n4, n5, n6, b);
    }

    public new void paint([JavaType(nameof(Graphics))] Reference r)
    {
        Graphics g = (Graphics)Jvm.ResolveObject(r);
        if (g == null)
            Jvm.Throw<NullPointerException>();
        if (Visible)
        {
            int n = g.getClipX() - this.anInt270;
            int n2 = g.getClipY() - this.anInt269;
            int n3 = g.getClipX() + g.getClipWidth() + this.anInt270;
            int n4 = g.getClipY() + g.getClipHeight() + this.anInt269;
            for (int anInt600 = Y, i = 0; i < this.anIntArrayArray264.Length; ++i, anInt600 += this.anInt269)
            {
                for (int anInt601 = X, length = this.anIntArrayArray264[i].Length, j = 0; j < length; ++j, anInt601 += this.anInt270)
                {
                    int animatedTile;
                    if ((animatedTile = this.anIntArrayArray264[i][j]) != 0 && anInt601 >= n && anInt601 <= n3 && anInt600 >= n2)
                    {
                        if (anInt600 <= n4)
                        {
                            if (animatedTile < 0)
                            {
                                animatedTile = this.getAnimatedTile(animatedTile);
                            }
                            g.drawRegion(this.anImage265, this.anIntArray266[animatedTile], this.anIntArray267[animatedTile], this.anInt270, this.anInt269, 0, anInt601, anInt600, 20);
                        }
                    }
                }
            }
        }
    }

    private void method111([JavaType(nameof(Image))] Reference ranImage265, int anInt273, int anInt274, int anInt275, bool b)
    {
        Image anImage265 = (Image) Jvm.ResolveObject(ranImage265);
        this.anInt270 = anInt274;
        this.anInt269 = anInt275;
        int width = anImage265.getWidth();
        int height = anImage265.getHeight();
        this.anImage265 = ranImage265;
        this.anInt273 = anInt273;
        this.anIntArray266 = new int[this.anInt273];
        this.anIntArray267 = new int[this.anInt273];
        if (!b)
        {
            TiledLayer tiledLayer = this;
            int anInt276 = 0;
            while (true)
            {
                tiledLayer.anInt271 = anInt276;
                if (this.anInt271 >= this.anIntArrayArray264.Length)
                {
                    break;
                }
                int length = this.anIntArrayArray264[this.anInt271].Length;
                TiledLayer tiledLayer2 = this;
                int anInt277 = 0;
                while (true)
                {
                    tiledLayer2.anInt272 = anInt277;
                    if (this.anInt272 >= length)
                    {
                        break;
                    }
                    this.anIntArrayArray264[this.anInt271][this.anInt272] = 0;
                    tiledLayer2 = this;
                    anInt277 = this.anInt272 + 1;
                }
                tiledLayer = this;
                anInt276 = this.anInt271 + 1;
            }
            this.anIntArray268 = null;
        }
        int n = 1;
        int n3;
        int n2 = n3 = 0;
        while (true)
        {
            int n4 = n3;
            if (n2 >= height)
            {
                break;
            }
            int n6;
            int n5 = n6 = 0;
            while (true)
            {
                int n7 = n6;
                if (n5 >= width)
                {
                    break;
                }
                this.anIntArray266[n] = n7;
                this.anIntArray267[n] = n4;
                ++n;
                n5 = (n6 = n7 + anInt274);
            }
            n2 = (n3 = n4 + anInt275);
        }
    }
}