// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;
using Thread = java.lang.Thread;

namespace MahoTrans.Tests.Linker;

public class TestBasicMarshallingLink
{
    [Test]
    public void TestAll()
    {
        // preparing jvm
        var jvm = TestStaticRefReturnType.createJvm();
        jvm.AddClrClasses(new[] { typeof(TestCls) });
        var cls = jvm.GetClass("MahoTrans/Tests/Linker/TestCls");

        Assert.That(cls.Methods.Count, Is.EqualTo(10));

        Assert.That(GetDescriptor(cls, "R1"), Is.EqualTo("(Ljava/lang/Object;)V"));

        Assert.That(GetDescriptor(cls, "A1"), Is.EqualTo("([Ljava/lang/Object;)V"));
        Assert.That(GetDescriptor(cls, "A2"), Is.EqualTo("([Ljava/lang/Object;)V"));
        Assert.That(GetDescriptor(cls, "A3"), Is.EqualTo("([Ljava/lang/Object;)V"));

        Assert.That(GetDescriptor(cls, "S1"), Is.EqualTo("(Ljava/lang/String;)V"));
        Assert.That(GetDescriptor(cls, "S2"), Is.EqualTo("(Ljava/lang/String;)V"));

        Assert.That(GetDescriptor(cls, "M1"), Is.EqualTo("(I[ZLjava/lang/String;Ljava/lang/Thread;)V"));
        Assert.That(GetDescriptor(cls, "M2"), Is.EqualTo("(Ljava/lang/Object;[CJ[D)V"));

        Assert.That(GetDescriptor(cls, "C1"), Is.EqualTo("([[F)V"));
        Assert.That(GetDescriptor(cls, "C2"), Is.EqualTo("([[F)V"));
    }

    private string GetDescriptor(JavaClass cls, string name)
    {
        return cls.Methods.First(x => x.Key.Name == name).Key.Descriptor;
    }
}

public class TestCls : Object
{
    public static void R1(Reference r)
    {
    }

    public static void A1(Reference[] r)
    {
    }

    public static void A2([JavaType(typeof(Object))] Reference[] r)
    {
    }

    public static void A3([JavaType("[Ljava/lang/Object;")] Reference r)
    {
    }

    public static void S1([String] Reference r)
    {
    }

    public static void S2(string s)
    {
    }

    public static void M1(int a, bool[] b, string d, [JavaType(typeof(Thread))] Reference t)
    {
    }

    public static void M2(Reference o, char[] c, long l, double[] d)
    {
    }

    public static void C1([JavaType("[[F")] Reference r)
    {
    }

    public static void C2([JavaType("[F")] Reference[] r)
    {
    }
}