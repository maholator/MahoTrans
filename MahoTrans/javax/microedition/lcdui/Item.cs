// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Handles;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Item : Object
{
    [JavaIgnore] public DisplayableHandle Owner;

    [String] public Reference Label;

    [return: String]
    public Reference getLabel() => Label;

    public void setLabel([String] Reference label)
    {
        Label = label;
        NotifyToolkit();
    }

    [JavaIgnore]
    protected void NotifyToolkit()
    {
        if (Owner != default) Toolkit.Display.ItemUpdated(Owner, This);
    }


    public const int BUTTON = 2;
    public const int HYPERLINK = 1;
    public const int LAYOUT_2 = 16384;
    public const int LAYOUT_BOTTOM = 32;
    public const int LAYOUT_CENTER = 3;
    public const int LAYOUT_DEFAULT = 0;
    public const int LAYOUT_EXPAND = 2048;
    public const int LAYOUT_LEFT = 1;
    public const int LAYOUT_NEWLINE_AFTER = 512;
    public const int LAYOUT_NEWLINE_BEFORE = 256;
    public const int LAYOUT_RIGHT = 2;
    public const int LAYOUT_SHRINK = 1024;
    public const int LAYOUT_TOP = 16;
    public const int LAYOUT_VCENTER = 48;
    public const int LAYOUT_VEXPAND = 8192;
    public const int LAYOUT_VSHRINK = 4096;
    public const int PLAIN = 0;
}