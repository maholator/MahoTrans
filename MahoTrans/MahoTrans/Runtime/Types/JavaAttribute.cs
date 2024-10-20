// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
