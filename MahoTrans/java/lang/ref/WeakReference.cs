// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans.Native;

namespace java.lang.@ref;

public class WeakReference : Reference
{
    [InitMethod]
    public void Init(MahoTrans.Runtime.Reference r)
    {
        base.Init();
        StoredReference = r.Index;
    }
}
