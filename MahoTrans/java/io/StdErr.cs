// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

namespace java.io;

public class StdErr : OutputStream
{
    public new void write(int b) => Toolkit.System.PrintErr((byte)((uint)b & 0xFF));
}
