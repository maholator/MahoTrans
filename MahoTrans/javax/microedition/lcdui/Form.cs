// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Utils;

namespace javax.microedition.lcdui;

public class Form : Screen
{
    [JavaIgnore]
    public List<Reference> Items = new();

    public Reference StateListener;

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        queue.Enqueue(Items);
    }

    [InitMethod]
    public void Init([String] Reference title)
    {
        base.Init();
        setTitle(title);
    }

    [InitMethod]
    [JavaDescriptor("(Ljava/lang/String;[Ljavax/microedition/lcdui/Item;)V")]
    public void Init(Reference title, Reference items)
    {
        Init(title);
        var itemsArray = Jvm.ResolveArray<Reference>(items);
        foreach (var item in itemsArray)
            append(item);
    }

    public int append([JavaType(typeof(Item))] Reference item)
    {
        var i = Jvm.Resolve<Item>(item);
        if (i.IsAttached)
            Jvm.Throw<IllegalStateException>();
        i.AttachTo(this);
        Items.Add(item);
        Toolkit.Display.ContentUpdated(Handle);
        return Items.Count - 1;
    }

    public int append___text([String] Reference str)
    {
        var i = Jvm.Allocate<StringItem>();
        i.Init(Reference.Null, str);
        return append(i.This);
    }

    public int append___image([JavaType(typeof(Image))] Reference image)
    {
        var i = Jvm.Allocate<ImageItem>();
        i.Init(Reference.Null, image, 0, Reference.Null);
        return append(i.This);
    }

    public void delete(int n)
    {
        if (n < 0 || n >= Items.Count)
            Jvm.Throw<IndexOutOfBoundsException>();
        Jvm.Resolve<Item>(Items[n]).AttachTo(null);
        Items.RemoveAt(n);
        Toolkit.Display.ContentUpdated(Handle);
    }

    public void deleteAll()
    {
        foreach (var item in Items)
            Jvm.Resolve<Item>(item).AttachTo(null);
        Items.Clear();
        Toolkit.Display.ContentUpdated(Handle);
    }

    public int size() => Items.Count;

    public void setItemStateListener([JavaType(typeof(ItemStateListener))] Reference l) => StateListener = l;
}
