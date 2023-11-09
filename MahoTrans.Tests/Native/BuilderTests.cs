using java.util;
using MahoTrans.Builder;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace MahoTrans.Tests.Native;

public class BuilderTests
{
    public BuilderTests()
    {
        var jmb = new JavaMethodBody(19, 10);
        jmb.RawCode = _vectorLoopEqUncalced;
        _vectorLoopEq = jmb.Code;
    }

    private readonly Instruction[] _vectorLoopEq;

    private readonly Instruction[] _vectorLoopEqUncalced = new Instruction[]
    {
        new(JavaOpcode.aload_0),
        new(JavaOpcode.invokevirtual, new byte[] { 0, 0 }),
        new(JavaOpcode.astore_2),
        new(JavaOpcode.iconst_0),
        new(JavaOpcode.istore_3),
        new(JavaOpcode.@goto, new byte[] { 0, 20 }),

        // loop
        new(JavaOpcode.aload_2),
        new(JavaOpcode.invokevirtual, new byte[] { 0, 1 }),
        new(JavaOpcode.aload_1),
        new(JavaOpcode.swap),
        // target > el
        new(JavaOpcode.invokevirtual, new byte[] { 0, 2 }),
        // areEquals
        new(JavaOpcode.ifne, new byte[] { 0, 5 }),
        new(JavaOpcode.iload_3),
        new(JavaOpcode.ireturn),

        new(JavaOpcode.iinc, new byte[] { 3, 1 }),

        // condition
        new(JavaOpcode.aload_2),
        new(JavaOpcode.invokevirtual, new byte[] { 0, 3 }),
        new(JavaOpcode.ifne, (-21).Split()),

        // return -1 if nothing found
        new(JavaOpcode.iconst_m1),
        new(JavaOpcode.ireturn),
    };

    [Test]
    public void TestSimpleBranch()
    {
        var cls = new JavaClass { Name = "java/util/Vector" };
        Assert.That(cls.Constants, Is.Empty);
        var b = new JavaMethodBuilder(cls);
        Assert.That(b.Build(), Is.Empty);

        b.AppendLoadThis();
        b.AppendVirtcall("field", typeof(bool));
        var @if = b.AppendForwardGoto(JavaOpcode.ifeq);
        b.Append(JavaOpcode.iconst_2);
        var @else = b.AppendForwardGoto(JavaOpcode.@goto);
        b.BringLabel(@if);
        b.Append(JavaOpcode.iconst_3);
        b.BringLabel(@else);
        b.Append(JavaOpcode.ireturn);

        Instruction[] res = new[]
        {
            new Instruction(0, JavaOpcode.aload_0),
            new Instruction(1, JavaOpcode.invokevirtual, new byte[] { 0, 0 }),
            new Instruction(4, JavaOpcode.ifeq, 7.Split()),
            new Instruction(7, JavaOpcode.iconst_2),
            new Instruction(8, JavaOpcode.@goto, 4.Split()),
            new Instruction(11, JavaOpcode.iconst_3),
            new Instruction(12, JavaOpcode.ireturn)
        };

        Assert.That(b.Build(), Is.EquivalentTo(res));
    }

    [Test]
    public void TestLoop()
    {
        var cls = new JavaClass { Name = "java/util/Vector" };
        Assert.That(cls.Constants, Is.Empty);
        var b = new JavaMethodBuilder(cls);
        Assert.That(b.Build(), Is.Empty);

        b.AppendLoadThis();
        b.AppendVirtcall("elements", typeof(Enumeration));
        b.Append(JavaOpcode.astore_2);
        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.istore_3);

        var loop = b.BeginLoop(JavaOpcode.ifne);

        b.Append(JavaOpcode.aload_2);
        b.AppendVirtcall(nameof(ArrayEnumerator.nextElement), typeof(Object));
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.swap);
        b.AppendVirtcall(nameof(Object.equals), typeof(bool), typeof(Reference));
        var notEq = b.AppendForwardGoto(JavaOpcode.ifne);
        b.Append(JavaOpcode.iload_3);
        b.Append(JavaOpcode.ireturn);
        b.BringLabel(notEq);
        b.AppendInc(3, 1);

        b.BeginLoopCondition(loop);

        b.Append(JavaOpcode.aload_2);
        b.AppendVirtcall(nameof(ArrayEnumerator.hasMoreElements), typeof(bool));

        b.EndLoop(loop);

        b.Append(JavaOpcode.iconst_m1);
        b.Append(JavaOpcode.ireturn);

        Assert.That(b.Build(), Is.EquivalentTo(_vectorLoopEq));
    }
}