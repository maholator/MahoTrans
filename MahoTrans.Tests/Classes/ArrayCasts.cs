// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;
using MahoTrans.Runtime.Config;
using MahoTrans.ToolkitImpls.Stub;
using MahoTrans.Utils;

namespace MahoTrans.Tests.Classes;

[TestFixture]
public class ArrayCasts
{
    [Test]
    public void TestCast()
    {
        var jvm = new JvmState(StubToolkit.Create(), ExecutionManner.Unlocked);
        jvm.AddMahoTransLibrary();
        jvm.LinkAndLock();

        using (new JvmContext(jvm))
        {
            var primArr = jvm.WrapPrimitiveArray(new int[] { 2, 9, 2 });
            var wrapper = jvm.WrapReferenceArray(new Reference[] { primArr }, "[[I");

            Assert.That(wrapper.AsObject().JavaClass.Is(jvm.GetClass("java/lang/Object")));
            Assert.That(wrapper.AsObject().JavaClass.Is(jvm.GetClass("[[I")));
            Assert.That(wrapper.AsObject().JavaClass.Is(jvm.GetClass("[Ljava/lang/Object;")));
        }
    }
}
