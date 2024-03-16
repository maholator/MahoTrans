// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace javax.microedition.lcdui;

/// <summary>
/// Used to access choice state directly.
/// </summary>
public interface INativeChoice
{
    /// <summary>
    ///     Choice type.
    /// </summary>
    ChoiceType Type { get; }

    /// <summary>
    ///     Selected indexes. Used if list is not <see cref="ChoiceType.Multiple"/>.
    /// </summary>
    int SelectedIndex { get; set; }

    /// <summary>
    ///     Selected indexes. Used only in <see cref="ChoiceType.Multiple"/>.
    /// </summary>
    List<bool> SelectedIndexes { get; }

    List<List.ListItem> Items { get; }

    /// <summary>
    ///     Count of elements in the choice.
    /// </summary>
    int ItemsCount => Items.Count;

    void Invalidate();
}
