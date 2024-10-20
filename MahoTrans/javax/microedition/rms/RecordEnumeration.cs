// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans;
using MahoTrans.Native;

namespace javax.microedition.rms;

public interface RecordEnumeration : IJavaObject
{
    public int nextRecordId() => throw new AbstractCall();

    public bool hasNextElement() => throw new AbstractCall();

    public void destroy() => throw new AbstractCall();
}
