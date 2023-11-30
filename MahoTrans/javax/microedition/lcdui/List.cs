using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class List : Screen
{
    [JavaIgnore] public List<ListItem> Items = new();

    [InitMethod]
    public void Init([String] Reference title, int listType)
    {
        base.Init();
        setTitle(title);

        //todo check and assign listType
    }

    [InitMethod]
    public void Init([String] Reference title, int listType, Reference stringElements, Reference imageElements)
    {
        Init(title, listType);

        var strings = Jvm.ResolveArray<Reference>(stringElements);
        if (imageElements.IsNull)
        {
            foreach (var str in strings)
            {
                if (str.IsNull)
                    Jvm.Throw<NullPointerException>();
                Items.Add(new ListItem(str, Reference.Null));
            }

            return;
        }

        var images = Jvm.ResolveArray<Reference>(imageElements);
        if (images.Length != strings.Length)
            Jvm.Throw<IllegalArgumentException>();

        for (var i = 0; i < strings.Length; i++)
        {
            var str = strings[i];
            if (str.IsNull)
                Jvm.Throw<NullPointerException>();
            Items.Add(new ListItem(str, images[i]));
        }
    }

    public int append([String] Reference stringPart, [JavaType(typeof(Image))] Reference imagePart)
    {
        if (stringPart.IsNull)
            Jvm.Throw<NullPointerException>();
        var index = Items.Count;
        Items.Add(new ListItem(stringPart, imagePart));
        Toolkit.Display.ContentUpdated(Handle);
        return index;
    }

    public void delete(int elementNum)
    {
        if (elementNum < 0 || elementNum >= Items.Count)
            Jvm.Throw<IndexOutOfBoundsException>();
        Items.RemoveAt(elementNum);
        Toolkit.Display.ContentUpdated(Handle);
    }

    public void deleteAll()
    {
        Items.Clear();
        Toolkit.Display.ContentUpdated(Handle);
    }


    public int size() => Items.Count;

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        foreach (var item in Items)
        {
            queue.Enqueue(item.Text);
            queue.Enqueue(item.Image);
        }

        base.AnnounceHiddenReferences(queue);
    }

    public struct ListItem
    {
        public Reference Text;
        public Reference Image;

        public ListItem(Reference text, Reference image)
        {
            Text = text;
            Image = image;
        }
    }
}