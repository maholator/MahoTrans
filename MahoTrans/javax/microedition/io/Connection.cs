// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans;
using MahoTrans.Native;

namespace javax.microedition.io;

public interface Connection : IJavaObject
{
    public void close() => throw new AbstractCall();
}
