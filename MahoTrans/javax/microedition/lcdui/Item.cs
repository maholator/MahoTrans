// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Toolkits;
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
        if (Owner != default) Toolkit.Display.ContentUpdated(Owner);
    }


    public const int PLAIN = 0;
    public const int HYPERLINK = 1;
    public const int BUTTON = 2;
}