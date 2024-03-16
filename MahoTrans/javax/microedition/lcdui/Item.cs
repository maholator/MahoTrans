// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Handles;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Item : Object
{
    [JavaIgnore]
    public DisplayableHandle Owner;

    [String]
    public Reference Label;

    [JavaIgnore]
    public int Layout;

    public int PrefW, PrefH;

    [return: String]
    public Reference getLabel() => Label;

    public void setLabel([String] Reference label)
    {
        Label = label;
        NotifyToolkit();
    }

    public int getLayout() => Layout;

    public void setLayout(int layout)
    {
        Layout = layout;
        NotifyToolkit();
    }

    public int getPreferredWidth() => PrefW;

    public int getPreferredHeight() => PrefH;

    public void setPreferredSize(int width, int height)
    {
        if (width < -1)
            Jvm.Throw<IllegalArgumentException>();

        if (height < -1)
            Jvm.Throw<IllegalArgumentException>();

        PrefW = width;
        PrefH = height;
        NotifyToolkit();
    }

    public void notifyStateChanged()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Call this if you change anything on the item.
    /// </summary>
    [JavaIgnore]
    protected void NotifyToolkit()
    {
        if (Owner != default)
            Toolkit.Display.ItemUpdated(Owner, This);
    }

    public const int PLAIN = 0;
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
}
