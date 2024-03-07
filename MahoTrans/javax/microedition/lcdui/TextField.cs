// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class TextField : Item, HasText
{
    [JavaIgnore]
    public string Content = string.Empty;

    public int MaxSize { get; set; }

    [InitMethod]
    public void Init([String] Reference label, [String] Reference text, int maxSize, int constraints)
    {
        if (maxSize <= 0)
            Jvm.Throw<IllegalArgumentException>();
        base.Init();
        Label = label;
        Content = Jvm.ResolveStringOrNull(text) ?? string.Empty;
        MaxSize = maxSize;
    }

    string HasText.Text
    {
        get => Content;
        set => Content = value;
    }

    public const int ANY = 0;
    public const int CONSTRAINT_MASK = 65535;
    public const int DECIMAL = 5;
    public const int EMAILADDR = 1;
    public const int INITIAL_CAPS_SENTENCE = 2097152;
    public const int INITIAL_CAPS_WORD = 1048576;
    public const int NON_PREDICTIVE = 524288;
    public const int NUMERIC = 2;
    public const int PASSWORD = 65536;
    public const int PHONENUMBER = 3;
    public const int SENSITIVE = 262144;
    public const int UNEDITABLE = 131072;
    public const int URL = 4;
}
