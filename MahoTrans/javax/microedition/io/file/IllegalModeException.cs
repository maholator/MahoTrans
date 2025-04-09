// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using IOException = java.io.IOException;

namespace javax.microedition.io.file;

public class IllegalModeException : IOException
{
    [InitMethod]
    public new void Init()
    {
        base.Init();
    }

    [InitMethod]
    public new void Init([String] Reference message)
    {
        base.Init(message);
    }
}