// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;

namespace javax.microedition.lcdui;

[JavaInterface]
public interface Choice
{
    int SelectedIndex { get; set; }
    bool[] SelectedIndices { get; set; }
}