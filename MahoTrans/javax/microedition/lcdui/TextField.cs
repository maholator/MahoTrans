// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class TextField : Item
{
    [JavaIgnore] public string Content = string.Empty;

    [InitMethod]
    public void Init([String] Reference label, [String] Reference text, int maxSize, int constraints)
    {
        Label = label;
        Content = Jvm.ResolveStringOrDefault(text) ?? string.Empty;
    }
}