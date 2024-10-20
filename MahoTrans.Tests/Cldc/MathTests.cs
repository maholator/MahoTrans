// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Tests.Cldc;

public class MathTests : JarTestBase
{
    [SetUp]
    public void Setup() => DetachHeap();

    [Test]
    public void TestBasic() => Load("TestBasic").Run();

    [Test]
    public void WideIinc() => Load("WideIinc").Run();

    public override string ClassName => "MathTests";
}
