// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.lcdui;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace com.nokia.mid.ui;

public class DirectUtils : Object
{
    [return: JavaType(typeof(DirectGraphics))]
    public static Reference getDirectGraphics([JavaType(typeof(Graphics))] Reference g) => g;
}