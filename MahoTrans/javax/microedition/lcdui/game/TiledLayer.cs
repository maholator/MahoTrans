// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui.game;

public class TiledLayer : Layer
{
    private int CellHeight;
    private int CellWidth;
    private int Rows;
    private int Columns;
    [JavaIgnore] private int[][] Cells = null!;
    public Reference Image;
    private int StaticTilesCount;
    [JavaIgnore] public int[] CellsX = null!;
    [JavaIgnore] public int[] CellsY = null!;
    [JavaIgnore] private int[]? AnimatedTiles = null!;
    private int AnimatedTilesCount;

    [InitMethod]
    public void Init(int columns, int rows, [JavaType(nameof(lcdui.Image))] Reference r, int tileWidth, int tileHeight)
    {
        Image image = Jvm.Resolve<Image>(r);
        if (image.getWidth() % tileWidth != 0 || image.getHeight() % tileHeight != 0)
            Jvm.Throw<IllegalArgumentException>();
        Image = r;
        Columns = columns;
        Rows = rows;
        Cells = new int[rows][];
        for(int i = 0; i < rows; i++)
        {
            Cells[i] = new int[columns];
        }
        SetImage(image, image.getWidth() / tileWidth * (image.getHeight() / tileHeight) + 1, tileWidth, tileHeight, true);
    }

    public int createAnimatedTile(int staticTileIndex)
    {
        if (staticTileIndex < 0 || staticTileIndex >= StaticTilesCount)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        if (AnimatedTiles == null)
        {
            AnimatedTiles = new int[4];
            AnimatedTilesCount = 1;
        }
        else if (AnimatedTilesCount == AnimatedTiles.Length)
        {
            int[] tmp = new int[AnimatedTiles.Length * 2];
            System.Array.Copy(AnimatedTiles, 0, tmp, 0, AnimatedTiles.Length);
            AnimatedTiles = tmp;
        }
        AnimatedTiles[AnimatedTilesCount++] = staticTileIndex;
        return -(AnimatedTilesCount - 1);
    }

    public void setAnimatedTile(int animatedTileIndex, int staticTileIndex)
    {
        if (staticTileIndex < 0 || staticTileIndex >= StaticTilesCount)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        animatedTileIndex = -animatedTileIndex;
        if (AnimatedTiles == null || animatedTileIndex <= 0 || animatedTileIndex >= AnimatedTilesCount)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        AnimatedTiles[animatedTileIndex] = staticTileIndex;
    }

    public int getAnimatedTile(int animatedTileIndex)
    {
        animatedTileIndex = -animatedTileIndex;
        if (AnimatedTiles == null || animatedTileIndex <= 0 || animatedTileIndex >= AnimatedTilesCount)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        return AnimatedTiles[animatedTileIndex];
    }

    public void setCell(int col, int row, int tileIndex)
    {
        if (col < 0 || col >= Columns || row < 0 || row >= Rows)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        if (tileIndex > 0)
        {
            if (tileIndex >= StaticTilesCount)
            {
                Jvm.Throw<IndexOutOfBoundsException>();
            }
        }
        else if (tileIndex < 0 && (AnimatedTiles == null || -tileIndex >= AnimatedTilesCount))
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        Cells[row][col] = tileIndex;
    }

    public int getCell(int col, int row)
    {
        if (col < 0 || col >= Columns || row < 0 || row >= Rows)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        return Cells[row][col];
    }

    public void fillCells(int col, int row, int numCols, int numRows, int tileIndex)
    {
        if (col < 0 || col >= Columns || row < 0 || row >= Rows || numCols < 0 || col + numCols > Columns || numRows < 0 || row + numRows > Rows)
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        if (tileIndex > 0)
        {
            if (tileIndex >= StaticTilesCount)
            {
                Jvm.Throw<IndexOutOfBoundsException>();
            }
        }
        else if (tileIndex < 0 && (AnimatedTiles == null || -tileIndex >= AnimatedTilesCount))
        {
            Jvm.Throw<IndexOutOfBoundsException>();
        }
        for (int i = row; i < row + numRows; ++i)
        {
            for (int j = col; j < col + numCols; ++j)
            {
                Cells[i][j] = tileIndex;
            }
        }
    }

    public int getCellWidth() => CellWidth;

    public int getCellHeight() => CellHeight;

    public int getColumns() => Columns;

    public int getRows() => Rows;

    public void setStaticTileSet([JavaType(nameof(lcdui.Image))] Reference r, int tileWidth, int tileHeight)
    {
        Image image = Jvm.Resolve<Image>(r);
        if (tileWidth < 1 || tileHeight < 1 || image.getWidth() % tileWidth != 0 || image.getHeight() % tileHeight != 0)
        {
            Jvm.Throw<IllegalArgumentException>();
        }
        Image = r;
        Width = Columns * tileWidth;
        Height = Rows * tileHeight;
        int n3 = (image.getWidth() / tileWidth * (image.getHeight() / tileHeight)) + 1;
        SetImage(image, n3, tileWidth, tileHeight, n3 >= StaticTilesCount);
    }

    public new void paint([JavaType(nameof(Graphics))] Reference r)
    {
        Graphics g = Jvm.Resolve<Graphics>(r);
        if (g == null)
            Jvm.Throw<NullPointerException>();
        if (Visible)
        {
            int n = g.getClipX() - CellWidth;
            int n2 = g.getClipY() - CellHeight;
            int n3 = g.getClipX() + g.getClipWidth() + CellWidth;
            int n4 = g.getClipY() + g.getClipHeight() + CellHeight;
            for (int y = Y, i = 0; i < Cells.Length; ++i, y += CellHeight)
            {
                for (int x = X, length = Cells[i].Length, j = 0; j < length; ++j, x += CellWidth)
                {
                    int animatedTile;
                    if ((animatedTile = Cells[i][j]) != 0 && x >= n && x <= n3 && y >= n2)
                    {
                        if (y <= n4)
                        {
                            if (animatedTile < 0)
                            {
                                animatedTile = getAnimatedTile(animatedTile);
                            }
                            g.drawRegion(Image, CellsX[animatedTile], CellsY[animatedTile], CellWidth, CellHeight, 0, x, y, 20);
                        }
                    }
                }
            }
        }
    }

    [JavaIgnore]
    private void SetImage(Image image, int tilesCount, int tileWidth, int tileHeight, bool b)
    {
        CellWidth = tileWidth;
        CellHeight = tileHeight;
        int width = image.getWidth();
        int height = image.getHeight();
        StaticTilesCount = tilesCount;
        CellsX = new int[StaticTilesCount];
        CellsY = new int[StaticTilesCount];
        if (!b)
        {
            int anInt276 = 0;
            while (true)
            {
                Rows = anInt276;
                if (Rows >= Cells.Length)
                {
                    break;
                }
                int length = Cells[Rows].Length;
                int anInt277 = 0;
                while (true)
                {
                    Columns = anInt277;
                    if (Columns >= length)
                    {
                        break;
                    }
                    anInt277 = Columns + 1;
                }
                anInt276 = Rows + 1;
            }
            AnimatedTiles = null;
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
                CellsX[n] = n7;
                CellsY[n] = n4;
                ++n;
                n5 = (n6 = n7 + tileWidth);
            }
            n2 = (n3 = n4 + tileHeight);
        }
    }
}