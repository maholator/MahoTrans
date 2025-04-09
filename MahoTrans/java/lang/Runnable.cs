// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans;
using MahoTrans.Native;

namespace java.lang;

public interface Runnable : IJavaObject
{
    public void run() => throw new AbstractCall();
}
