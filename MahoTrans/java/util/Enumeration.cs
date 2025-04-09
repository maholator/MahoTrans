// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.util;

public interface Enumeration : IJavaObject
{
    public bool hasMoreElements() => throw new AbstractCall();

    public Reference nextElement() => throw new AbstractCall();
}
