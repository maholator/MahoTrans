// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.io.file;

public class FileConnectionImpl : Object, FileConnection
{
    [InitMethod]
    public new void Init()
    {
        base.Init();
    }

    [JavaIgnore]
    public void Open(string url)
    {
    }


}
