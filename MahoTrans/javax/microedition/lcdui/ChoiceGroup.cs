// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class ChoiceGroup : Item, Choice, INativeChoice
{
    public List<List.ListItem> Items { get; } = new();

    public ChoiceType Type { get; set; }

    public int SelectedIndex { get; set; }

    public List<bool> SelectedIndexes { get; } = new();

    void INativeChoice.Invalidate() => NotifyToolkit();

    [InitMethod]
    public void Init([String] Reference label, int listType)
    {
        base.Init();
        Label = label;
        if (listType != 1 && listType != 2 && listType != 4)
            Jvm.Throw<IllegalArgumentException>();
        Type = (ChoiceType)listType;
    }

    [InitMethod]
    public void Init([String] Reference label, int listType, [String] Reference[] stringElements,
        [JavaType(typeof(Image))] Reference[]? imageElements)
    {
        Init(label, listType);
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

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        foreach (var item in Items)
            item.AnnounceHiddenReferences(queue);

        base.AnnounceHiddenReferences(queue);
    }
}
