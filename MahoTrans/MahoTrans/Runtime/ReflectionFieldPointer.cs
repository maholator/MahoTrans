// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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