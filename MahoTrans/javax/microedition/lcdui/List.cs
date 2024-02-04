// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class List : Screen, Choice
{
    [JavaIgnore] public List<ListItem> Items = new();

    public ChoiceType Type;

    [JavaIgnore] public Reference ImplicitSelectCommand;

    public int SelectedItem;

    int Choice.SelectedIndex
    {
        get => SelectedItem;
        set => SelectedItem = value;
    }

    bool[] Choice.SelectedIndices
    {
        get => SelectedMap.ToArray();
        set => SelectedMap = value.ToList();
    }

    [JavaIgnore] public List<bool> SelectedMap = new();

    [ClassInit]
    public static void ClInit()
    {
        var select = Jvm.AllocateObject<Command>();
        select.Init(Jvm.AllocateString(""), Command.SCREEN, 0);
        NativeStatics.ListSelectCommand = select.This;
    }

    [InitMethod]
    public void Init([String] Reference title, int listType)
    {
        base.Init();
        setTitle(title);
        if (listType < 1 || listType > 3)
            Jvm.Throw<IllegalArgumentException>();
        Type = (ChoiceType)listType;
        ImplicitSelectCommand = NativeStatics.ListSelectCommand;
        // invalidate is necessary to notify toolkit about non-null implicit command
        Toolkit.Display.CommandsUpdated(Handle, Commands, ImplicitSelectCommand);
    }

    [InitMethod]
    public void Init([String] Reference title, int listType, [JavaType("[Ljava/lang/String;")] Reference stringElements,
        [JavaType("[Ljavax/microedition/lcdui/Image;")]
        Reference imageElements)
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
                SelectedMap.Add(false);
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
            SelectedMap.Add(false);
        }
    }

    public int append([String] Reference stringPart, [JavaType(typeof(Image))] Reference imagePart)
    {
        if (stringPart.IsNull)
            Jvm.Throw<NullPointerException>();
        var index = Items.Count;
        Items.Add(new ListItem(stringPart, imagePart));
        SelectedMap.Add(false);
        Toolkit.Display.ContentUpdated(Handle);
        return index;
    }

    public void delete(int elementNum)
    {
        if (elementNum < 0 || elementNum >= Items.Count)
            Jvm.Throw<IndexOutOfBoundsException>();
        Items.RemoveAt(elementNum);
        SelectedMap.RemoveAt(elementNum);
        Toolkit.Display.ContentUpdated(Handle);
    }

    public void deleteAll()
    {
        Items.Clear();
        SelectedMap.Clear();
        Toolkit.Display.ContentUpdated(Handle);
    }

    public void set(int elementNum, [String] Reference stringPart, [JavaType(typeof(Image))] Reference imagePart)
    {
        if (elementNum < 0 || elementNum >= Items.Count)
            Jvm.Throw<IndexOutOfBoundsException>();
        if (stringPart.IsNull)
            Jvm.Throw<NullPointerException>();
        Items[elementNum] = new ListItem(stringPart, imagePart);
        Toolkit.Display.ContentUpdated(Handle);
    }

    public int size() => Items.Count;

    public new void addCommand([JavaType(typeof(Command))] Reference cmd)
    {
        if (cmd.IsNull)
            Jvm.Throw<NullPointerException>();
        if (Commands.Contains(cmd))
            return;
        Commands.Add(cmd);
        Toolkit.Display.CommandsUpdated(Handle, Commands, ImplicitSelectCommand);
    }

    public new void removeCommand([JavaType(typeof(Command))] Reference cmd)
    {
        if (cmd.IsNull)
            return;
        if (ImplicitSelectCommand == cmd)
            ImplicitSelectCommand = Reference.Null;
        Commands.Remove(cmd);
        Toolkit.Display.CommandsUpdated(Handle, Commands, ImplicitSelectCommand);
    }

    public void setSelectCommand([JavaType(typeof(Command))] Reference cmd)
    {
        // adding old command to the main list
        if (!ImplicitSelectCommand.IsNull)
            Commands.Add(ImplicitSelectCommand);

        // setting new select command
        ImplicitSelectCommand = cmd;

        // if it is in main list, it must not be there.
        Commands.Remove(cmd);

        Toolkit.Display.CommandsUpdated(Handle, Commands, ImplicitSelectCommand);
    }

    public int getSelectedIndex()
    {
        if (Type == ChoiceType.Multiple)
            return -1;

        return SelectedItem;
    }

    public void setSelectedIndex(int index, bool state)
    {
        if (Type == ChoiceType.Multiple)
        {
            SelectedMap[index] = state;
            Toolkit.Display.ContentUpdated(Handle);
            return;
        }

        if (state)
        {
            SelectedItem = index;
            Toolkit.Display.ContentUpdated(Handle);
        }
    }

    [JavaIgnore]
    public void SetSelectedFlags(bool[] selectedArray)
    {
        var count = SelectedMap.Count;
        if (selectedArray.Length < count)
            Jvm.Throw<IllegalArgumentException>();

        if (Type == ChoiceType.Multiple)
        {
            SelectedMap.Clear();
            SelectedMap.AddRange(selectedArray.Take(count));
        }
        else
        {
            SelectedItem = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                if (selectedArray[i])
                {
                    SelectedItem = i;
                    break;
                }
            }
        }

        Toolkit.Display.ContentUpdated(Handle);
    }

    public void setSelectedFlags([JavaType("[Z")] Reference flags)
    {
        SetSelectedFlags(Jvm.ResolveArray<bool>(flags));
    }

    public int getSelectedFlags([JavaType("[Z")] Reference flags)
    {
        var arr = Jvm.ResolveArray<bool>(flags);
        if (arr.Length < Items.Count)
            Jvm.Throw<IllegalArgumentException>();
        if (Type == ChoiceType.Multiple)
        {
            var count = 0;
            for (var i = 0; i < Items.Count; i++)
            {
                arr[i] = SelectedMap[i];
                if (SelectedMap[i])
                    count++;
            }

            return count;
        }

        for (var i = 0; i < arr.Length; i++)
        {
            arr[i] = false;
        }

        arr[SelectedItem] = true;

        return 1;
    }

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