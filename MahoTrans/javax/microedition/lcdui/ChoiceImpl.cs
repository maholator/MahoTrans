// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

/// <summary>
///     Common implementation for <see cref="List" /> and <see cref="ChoiceGroup" />.
/// </summary>
public static class ChoiceImpl
{
    public static void Initialize(this INativeChoice choice, Reference[] stringElements, Reference[]? imageElements)
    {
        var items = choice.Items;
        var sel = choice.SelectedIndexes;
        choice.SelectedIndex = 0;
        items.Clear();
        sel.Clear();

        if (imageElements == null)
        {
            foreach (var str in stringElements)
            {
                if (str.IsNull)
                    Jvm.Throw<NullPointerException>();
                items.Add(new List.ListItem(str, Reference.Null));
                sel.Add(false);
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
            items.Add(new List.ListItem(str, imageElements[i]));
            sel.Add(false);
        }
    }

    public static int GetSelected(this INativeChoice choice)
    {
        if (choice.Type == ChoiceType.Multiple)
            return -1;

        return choice.SelectedIndex;
    }

    public static void SetSelected(this INativeChoice choice, int index, bool state)
    {
        if (choice.Type == ChoiceType.Multiple)
        {
            choice.SelectedIndexes[index] = state;
            choice.Invalidate();
            return;
        }

        if (state)
        {
            choice.SelectedIndex = index;
            choice.Invalidate();
        }
    }

    public static int GetSelectedFlags(this INativeChoice choice, bool[] flags)
    {
        var count = choice.ItemsCount;
        if (flags.Length < count)
            Jvm.Throw<IllegalArgumentException>();

        var map = choice.SelectedIndexes;

        for (var i = 0; i < flags.Length; i++)
            flags[i] = false;

        if (count == 0)
            return 0;

        if (choice.Type != ChoiceType.Multiple)
        {
            flags[choice.SelectedIndex] = true;
            return 1;
        }

        var selectedCount = 0;
        for (var i = 0; i < count; i++)
        {
            flags[i] = map[i];
            if (map[i])
                selectedCount++;
        }

        return selectedCount;
    }

    public static void SetSelectedFlags(this INativeChoice choice, bool[] flags)
    {
        var count = choice.ItemsCount;
        if (flags.Length < count)
            Jvm.Throw<IllegalArgumentException>();

        var map = choice.SelectedIndexes;

        if (choice.Type == ChoiceType.Multiple)
        {
            map.Clear();
            map.AddRange(flags.Take(count));
        }
        else
        {
            choice.SelectedIndex = 0;
            for (int i = 0; i < count; i++)
            {
                if (flags[i])
                {
                    choice.SelectedIndex = i;
                    break;
                }
            }
        }

        choice.Invalidate();
    }

    public static void SetItem(this INativeChoice choice, int index, Reference text, Reference image)
    {
        if (text.IsNull)
            Jvm.Throw<NullPointerException>();

        choice.Items[index].Text = text;
        choice.Items[index].Image = image;
        choice.Invalidate();
    }

    private static JvmState Jvm => JvmContext.Jvm!;
}
