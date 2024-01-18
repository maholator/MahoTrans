// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.security;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.ToolkitImpls.Ams;
using MahoTrans.ToolkitImpls.Clocks;
using MahoTrans.ToolkitImpls.Dummy;
using MahoTrans.ToolkitImpls.Loggers;
using MahoTrans.ToolkitImpls.Rms;
using MahoTrans.ToolkitImpls.Systems;
using Object = java.lang.Object;

namespace MahoTrans.Tests.Linker;

public class TestStaticRefReturnType
{
    [Test]
    public void Test()
    {
        // preparing jvm
        var jvm = new JvmState(
            new ToolkitCollection(new DummySystem(), new RealTimeClock(), null!, new DummyFonts(), null!,
                new AmsEventHub(),
                new InMemoryRms(), null!)
            {
                Logger = new ConsoleLogger(),
                LoadLogger = new ConsoleLogger(),
            },
            ExecutionManner.Unlocked)
        {
            OnOverflow = AllocatorBehaviourOnOverflow.ThrowOutOfMem,
            UseBridgesForFields = false
        };
        jvm.AddClrClasses(typeof(JavaRunner).Assembly);

        // loading test class
        jvm.AddClrClasses(new[] { typeof(TestClass) });

        var cls = jvm.GetClass("MahoTrans/Tests/Linker/TestClass");

        Assert.That(cls.Methods.Count, Is.EqualTo(1));
        var method = cls.Methods.Single().Value;
        Assert.That(method.Descriptor.Descriptor, Is.EqualTo("(Ljava/lang/String;)Ljava/security/MessageDigest;"));
    }
}

public class TestClass : Object
{
    [return: JavaType(nameof(MessageDigest))]
    public static Reference getInstance([String] Reference algorithm)
    {
        return Reference.Null;
    }
}