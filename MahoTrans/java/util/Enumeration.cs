// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;

namespace java.util;

[JavaInterface]
public interface Enumeration
{
    public bool hasMoreElements() => throw new AbstractCall();
}