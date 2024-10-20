// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace javax.microedition.lcdui;

/// <summary>
///     Used to access choice state directly.
/// </summary>
public interface INativeChoice
{
    /// <summary>
    ///     Choice type.
    /// </summary>
    ChoiceType Type { get; }

    int FitPolicy { get; set; }

    /// <summary>
    ///     Selected indexes. Used if list is not <see cref="ChoiceType.Multiple" />.
    /// </summary>
    int SelectedIndex { get; set; }

    /// <summary>
    ///     Selected indexes. Used only in <see cref="ChoiceType.Multiple" />.
    /// </summary>
    List<bool> SelectedIndexes { get; }

    /// <summary>
    ///     Items in this choice.
    /// </summary>
    List<ChoiceItem> Items { get; }

    /// <summary>
    ///     Count of elements in the choice.
    /// </summary>
    int ItemsCount => Items.Count;

    /// <summary>
    ///     Notifies toolkit about change.
    /// </summary>
    void Invalidate();

    /// <summary>
    ///     Notifies application about change.
    /// </summary>
    void ReportChange();
}
