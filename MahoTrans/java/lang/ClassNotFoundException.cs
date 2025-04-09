// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class ClassNotFoundException : Exception
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
