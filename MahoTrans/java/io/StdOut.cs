// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace java.io;

public class StdOut : OutputStream
{
    public new void write(int b) => Toolkit.System.PrintOut((byte)((uint)b & 0xFF));
}
