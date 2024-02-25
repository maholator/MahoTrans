// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using MahoTrans.Runtime;
using MahoTrans.Utils;

namespace MahoTrans.Native;

[AttributeUsage(AttributeTargets.Field)]
[MeansImplicitUse]
public class NativeStaticAttribute : Attribute
{
    public Type FieldType;
    public Type OwnerType;
    public string Name;

    public NativeStaticAttribute(string name, Type fieldType, Type @class)
    {
        Name = name;
        FieldType = fieldType;
        OwnerType = @class;
    }

    public NameDescriptor AsDescriptor()
    {
        return new NameDescriptor(Name, FieldType.ToJavaDescriptor());
    }

    public string OwnerName => OwnerType.FullName!.Replace('.', '/');
}