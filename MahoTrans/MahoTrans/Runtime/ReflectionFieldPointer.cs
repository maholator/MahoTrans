// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using System.Reflection;
using MahoTrans.Runtime.Types;

namespace MahoTrans.Runtime;

public class ReflectionFieldPointer
{
    public FieldInfo Field;
    public JavaClass Class;

    public ReflectionFieldPointer(FieldInfo field, JavaClass @class)
    {
        Field = field;
        Class = @class;
    }
}
