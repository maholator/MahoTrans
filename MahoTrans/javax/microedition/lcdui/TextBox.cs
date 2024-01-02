// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class TextBox : Screen
{
    [JavaIgnore] public string Content = string.Empty;

    public int MaxSize;

    [InitMethod]
    public void Init([String] Reference title, [String] Reference text, int maxSize, int constraints)
    {
        base.Init();
        setTitle(title);
        Content = Jvm.ResolveStringOrDefault(text) ?? "";
        MaxSize = maxSize;
    }

    public int size() => Content.Length;

    [return: String]
    public Reference getString() => Jvm.InternalizeString(Content);

    public int getMaxSize() => MaxSize;
}