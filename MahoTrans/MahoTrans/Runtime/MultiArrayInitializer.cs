// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime.Types;

namespace MahoTrans.Runtime;

public class MultiArrayInitializer
{
    public int dimensions;
    public JavaClass type;

    public MultiArrayInitializer(int dimensions, JavaClass type)
    {
        this.dimensions = dimensions;
        this.type = type;
    }
}