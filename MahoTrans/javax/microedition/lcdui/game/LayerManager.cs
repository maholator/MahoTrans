// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.lcdui.game;

public class LayerManager : Object
{
    [JavaIgnore] public Reference[] Layers = null!;

    public int Size;
    public int X;
    public int Y;
    public int Width;
    public int Height;

    [InitMethod]
    public new void Init()
    {
        Layers = new Reference[4];
        setViewWindow(0, 0, Integer.MAX_VALUE, Integer.MAX_VALUE);
    }

    public void append([JavaType(typeof(Layer))] Reference layer)
    {
        remove(layer);
        insert(layer, Size);
    }

    public void insert([JavaType(typeof(Layer))] Reference layer, int index)
    {
        if (index < 0 || index > Size)
            Jvm.Throw<IndexOutOfBoundsException>();
        remove(layer);
        if(index == Size)
        {
            Reference[] tmp = new Reference[Size + 4];
            System.Array.Copy(Layers, tmp, Size);
            System.Array.Copy(Layers, index, tmp, index + 1, Size - index);
        }
        else
        {
            System.Array.Copy(Layers, index, Layers, index + 1, Size - index);
        }
        Layers[index] = layer;
        ++Size;
    }

    [return: JavaType(typeof(Layer))]
    public Reference getLayerAt(int index)
    {
        if (index < 0 || index >= Size)
            Jvm.Throw<IndexOutOfBoundsException>();
        return Layers[index];
    }

    public void remove([JavaType(typeof(Layer))] Reference layer)
    {
        if (layer.IsNull)
            Jvm.Throw<NullPointerException>();
        int i = Size;
        while (--i >= 0)
        {
            if (Layers[i] == layer)
            {
                System.Array.Copy(Layers, i + 1, Layers, i, Size - i - 1);
                Layers[--Size] = Reference.Null;
                break;
            }
        }
    }

    public void setViewWindow(int x, int y, int width, int height)
    {
        if (width < 0 || height < 0)
            Jvm.Throw<IllegalArgumentException>();
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int getSize() => Size;

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        foreach (var r in Layers)
            queue.Enqueue(r);
    }
}