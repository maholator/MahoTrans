// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans;
using MahoTrans.Native;

namespace javax.microedition.lcdui;

public interface ItemCommandListener : IJavaObject
{
    public void commandAction(Command command, Item item) => throw new AbstractCall();
}
