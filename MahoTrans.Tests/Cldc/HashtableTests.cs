// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Tests.Cldc;

public class HashtableTests : JarTestBase
{
    [SetUp]
    public void Setup() => DetachHeap();

    [Test]
    public void TestThrow()
    {
        var test = Load("TestThrow");
        Assert.Throws<JavaRuntimeError>(() => test.Run());
    }

    [Test]
    public void TestSimple() => Load("TestSimple").Run();

    [Test]
    public void TestOldValues() => Load("TestOldValues").Run();

    [Test]
    public void TestHas() => Load("TestHas").Run();

    [Test]
    public void TestRemove() => Load("TestRemove").Run();

    public override string ClassName => "HashtableTests";
}