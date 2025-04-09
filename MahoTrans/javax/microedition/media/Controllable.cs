// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.media;

public interface Controllable : IJavaObject
{
    public Control getControl(string controlType) => throw new AbstractCall();

    [return: JavaType(typeof(Control[]))]
    public Reference getControls() => throw new AbstractCall();
}
