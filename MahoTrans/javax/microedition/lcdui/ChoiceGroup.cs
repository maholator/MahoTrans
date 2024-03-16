// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class ChoiceGroup : Item, Choice
{
    [JavaIgnore]
    public List<List.ListItem> Items = new();

    public ChoiceType Type;

    public int SelectedItem;

    [JavaIgnore]
    public List<bool> SelectedMap = new();

    #region Impls for frontend

    int Choice.SelectedIndex
    {
        get => SelectedItem;
        set => SelectedItem = value;
    }

    bool[] Choice.SelectedIndixes
    {
        get => SelectedMap.ToArray();
        set => SelectedMap = value.ToList();
    }

    int Choice.ItemsCount => Items.Count;

    #endregion

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

        if (imageElements == null)
        {
            foreach (var str in stringElements)
            {
                if (str.IsNull)
                    Jvm.Throw<NullPointerException>();
                Items.Add(new List.ListItem(str, Reference.Null));
                SelectedMap.Add(false);
            }

            return;
        }

        if (imageElements.Length != stringElements.Length)
            Jvm.Throw<IllegalArgumentException>();

        for (var i = 0; i < stringElements.Length; i++)
        {
            var str = stringElements[i];
            if (str.IsNull)
                Jvm.Throw<NullPointerException>();
            Items.Add(new List.ListItem(str, imageElements[i]));
            SelectedMap.Add(false);
        }
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
            NotifyToolkit();
            return;
        }

        if (state)
        {
            SelectedItem = index;
            NotifyToolkit();
        }
    }

    public void setSelectedFlags(bool[] flags)
    {
        var count = SelectedMap.Count;
        if (flags.Length < count)
            Jvm.Throw<IllegalArgumentException>();

        if (Type == ChoiceType.Multiple)
        {
            SelectedMap.Clear();
            SelectedMap.AddRange(flags.Take(count));
        }
        else
        {
            SelectedItem = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                if (flags[i])
                {
                    SelectedItem = i;
                    break;
                }
            }
        }

        NotifyToolkit();
    }

    public int size() => Items.Count;

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        foreach (var item in Items)
            item.AnnounceHiddenReferences(queue);

        base.AnnounceHiddenReferences(queue);
    }
}
