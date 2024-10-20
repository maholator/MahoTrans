// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace java.lang.@ref;

public class Reference : Object
{
    public int StoredReference;

    public void clear() => StoredReference = 0;

    public MahoTrans.Runtime.Reference get() => StoredReference;
}
