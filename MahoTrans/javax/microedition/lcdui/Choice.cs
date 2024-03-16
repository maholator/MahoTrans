// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans;

namespace javax.microedition.lcdui;

public interface Choice : IJavaObject
{
    int SelectedIndex { get; set; }
    bool[] SelectedIndixes { get; set; }
    int ItemsCount { get; }
}
