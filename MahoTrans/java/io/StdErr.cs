// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace java.io;

public class StdErr : OutputStream
{
    public new void write(int b) => Toolkit.System.PrintErr((byte)((uint)b & 0xFF));
}