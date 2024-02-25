// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class TextBox : Screen, HasText
{
    [JavaIgnore] public string Content = string.Empty;

    public int MaxSize { get; set; }

    [InitMethod]
    public void Init([String] Reference title, [String] Reference text, int maxSize, int constraints)
    {
        if (maxSize <= 0)
            Jvm.Throw<IllegalArgumentException>();
        base.Init();
        setTitle(title);
        Content = Jvm.ResolveStringOrNull(text) ?? "";
        MaxSize = maxSize;
    }

    public int size() => Content.Length;

    [return: String]
    public Reference getString() => Jvm.InternalizeString(Content);

    public void setString([String] Reference text)
    {
        var t = Jvm.ResolveStringOrNull(text) ?? "";
        if (t.Length > MaxSize)
            Jvm.Throw<IllegalArgumentException>();
        Content = t;
        Toolkit.Display.ContentUpdated(Handle);
    }

    public int getMaxSize() => MaxSize;

    string HasText.Text
    {
        get => Content;
        set => Content = value;
    }
}