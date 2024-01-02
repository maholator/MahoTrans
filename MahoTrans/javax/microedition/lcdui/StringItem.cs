// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class StringItem : Item
{
    [String] public Reference Text;

    [JavaType(typeof(Font))] public Reference Font;

    public int Appearance;

    [InitMethod]
    public void Init([String] Reference label, [String] Reference text)
    {
        Label = label;
        Text = text;
    }

    [InitMethod]
    public void Init([String] Reference label, [String] Reference text, int appearanceMode)
    {
        base.Init();
        Label = label;
        Text = text;
        if (appearanceMode < 0 || appearanceMode > 2)
            Jvm.Throw<IllegalArgumentException>();
        Appearance = appearanceMode;
    }

    [return: String]
    public Reference getText() => Text;

    public void setText([String] Reference text)
    {
        Text = text;
        NotifyToolkit();
    }

    [return: JavaType(typeof(Font))]
    public Reference getFont() => Font;

    public void setFont([JavaType(typeof(Font))] Reference font)
    {
        Font = font;
        NotifyToolkit();
    }

    public int getAppearanceMode() => Appearance;
}