// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.security;

public class DigestException : GeneralSecurityException
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