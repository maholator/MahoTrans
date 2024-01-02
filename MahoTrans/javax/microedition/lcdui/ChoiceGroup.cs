// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class ChoiceGroup : Item
{
    [JavaIgnore] public List<List.ListItem> Items = new();

    public ChoiceType Type;

    public int SelectedItem = 0;

    [JavaIgnore] public List<bool> SelectedMap = new();

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
                Items.Add(new List.ListItem(str, Reference.Null));
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
            Items.Add(new List.ListItem(str, images[i]));
            SelectedMap.Add(false);
        }
    }

    public int getSelectedIndex()
    {
        if (Type == ChoiceType.Multiple)
            return -1;

        return SelectedItem;
    }
}