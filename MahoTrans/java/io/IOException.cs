// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using Exception = java.lang.Exception;

namespace java.io;

public class IOException : Exception
{
    [InitMethod]
    public new void Init([String] Reference msg)
    {
        base.Init(msg);
    }

    [InitMethod]
    public new void Init()
    {
        base.Init();
    }
}
