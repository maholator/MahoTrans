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
[InvocationCount(65536)]
[MaxIterationCount(40)]
public class AllocBenchmark
{
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
        return form.This;
    }

    [Benchmark]
    public Reference AllocByName()
    {
        return JvmContext.Jvm!.AllocateObject(_formClass);
    }
}