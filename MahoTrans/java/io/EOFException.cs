// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.io;

public class EOFException : IOException
{
    [InitMethod]
    public new void Init([String] Reference msg)
    {
    }

    [InitMethod]
    public new void Init()
    {
    }
}