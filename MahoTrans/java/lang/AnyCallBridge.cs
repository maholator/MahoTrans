// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace java.lang;

/// <summary>
///     Use this runnable to call any virtual method on any object. Supports only ()V for now.
/// </summary>
public class AnyCallBridge : Object, Runnable
{
    public Reference Target;
    [String] public Reference MethodName;
    [String] public Reference MethodDescriptor;

    [InitMethod]
    public void Init(Reference target, [String] Reference name, [String] Reference descr)
    {
        Target = target;
        MethodName = name;
        MethodDescriptor = descr;
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody run(JavaClass cls)
    {
        return new JavaMethodBody(3, 1)
        {
            RawCode = new[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, cls.PushConstant(new NameDescriptorClass(
                    nameof(Target),
                    typeof(Object),
                    GetType()
                )).Split()),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, cls.PushConstant(new NameDescriptorClass(
                    nameof(MethodName),
                    typeof(String),
                    GetType()
                )).Split()),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, cls.PushConstant(new NameDescriptorClass(
                    nameof(MethodDescriptor),
                    typeof(String),
                    GetType()
                )).Split()),
                new Instruction(JavaOpcode._invokeany),
                new Instruction(JavaOpcode.@return),
            }
        };
    }
}