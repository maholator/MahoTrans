// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

namespace MahoTrans.Runtime.Types;

public class JavaAttribute
{
    public readonly string Type;
    public byte[] Data = Array.Empty<byte>();

    public JavaAttribute(string type)
    {
        Type = type;
    }
}
