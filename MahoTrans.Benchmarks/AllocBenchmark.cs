// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using javax.microedition.lcdui;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Config;
using MahoTrans.Runtime.Types;
using MahoTrans.ToolkitImpls.Stub;

namespace MahoTrans.Benchmarks;

[MemoryDiagnoser]
[MaxIterationCount(20)]
public class AllocBenchmark
{
#pragma warning disable CS0618 // Type or member is obsolete

    [GlobalSetup]
    public void Setup()
    {
        var j = new JvmState(StubToolkit.Create(), ExecutionManner.Unlocked);
        j.AddMahoTransLibrary();
        _ = new JvmContext(j);
        _formClass = j.Classes["javax/microedition/lcdui/Form"];
    }

    private JavaClass _formClass = null!;


    [Benchmark]
    public Reference AllocGeneric()
    {
        var form = JvmContext.Jvm!.Allocate<Form>();
        JvmContext.Jvm.ForceDeleteObject(form.This);
        return form.This;
    }

    [Benchmark]
    public Reference AllocByName()
    {
        var r = JvmContext.Jvm!.AllocateObject(_formClass);
        JvmContext.Jvm.ForceDeleteObject(r);
        return r;
    }
#pragma warning restore CS0618 // Type or member is obsolete
}