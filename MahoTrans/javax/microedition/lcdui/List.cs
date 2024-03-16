// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class List : Screen, Choice, INativeChoice
{
    public List<ChoiceItem> Items { get; } = new();

    public ChoiceType Type { get; set; }

    public int FitPolicy { get; set; }

    public int SelectedIndex { get; set; }

    public List<bool> SelectedIndexes { get; } = new();

    void INativeChoice.Invalidate() => Toolkit.Display.ContentUpdated(Handle);

    [JavaIgnore]
    public Reference ImplicitSelectCommand;

    [ClassInit]
    public static void ClInit()
    {
        var select = Jvm.Allocate<Command>();
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
    public void Init([String] Reference title, int listType, [String] Reference[] stringElements,
        [JavaType(typeof(Image))] Reference[]? imageElements)
    {
        Init(title, listType);
        this.Initialize(stringElements, imageElements);
    }

    public int getSelectedIndex() => this.GetSelected();

    public void setSelectedIndex(int index, bool state) => this.SetSelected(index, state);

    public int getSelectedFlags(bool[] flags) => this.GetSelectedFlags(flags);

    public void setSelectedFlags(bool[] flags) => this.SetSelectedFlags(flags);

    public int size() => Items.Count;

    [return: JavaType(typeof(Image))]
    public Reference getImage(int index) => Items[index].Image;

    [return: String]
    public Reference getString(int index) => Items[index].Text;

    [return: JavaType(typeof(Font))]
    public Reference getFont(int index) => Items[index].Font;

    public void set(int index, [String] Reference text, [JavaType(typeof(Image))] Reference image) =>
        this.SetItem(index, text, image);

    public void setFont(int index, [JavaType(typeof(Font))] Reference font)
    {
        Items[index].Font = font;
        Toolkit.Display.ContentUpdated(Handle);
    }

    public bool isSelected(int index) => this.GetItemState(index);

    public int getFitPolicy() => FitPolicy;

    public void setFitPolicy(int policy) => this.SetFitPolicy(policy);

    public void deleteAll() => this.Clear();

    public void delete(int index) => this.RemoveAt(index);

    public void insert(int index, [String] Reference text, [JavaType(typeof(Image))] Reference image) =>
        this.Insert(index, text, image);

    public int append([String] Reference text, [JavaType(typeof(Image))] Reference image) =>
        this.Add(text, image);

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

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        foreach (var item in Items)
            item.AnnounceHiddenReferences(queue);

        base.AnnounceHiddenReferences(queue);
    }
}
