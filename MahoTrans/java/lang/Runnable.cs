// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans;
using MahoTrans.Native;

namespace java.lang;

public interface Runnable : IJavaObject
{
    public void run() => throw new AbstractCall();
}
