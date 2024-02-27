// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using javax.microedition.lcdui;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Config;
using MahoTrans.ToolkitImpls.Stub;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace MahoTrans.Benchmarks;

[MemoryDiagnoser]
[InvocationCount(2097152)]
[MaxIterationCount(20)]
public class ResolveBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        var j = new JvmState(StubToolkit.Create(), ExecutionManner.Unlocked);
        j.AddMahoTransLibrary();
        _ = new JvmContext(j);
        for (int i = 0; i < _refs.Length; i++)
        {
            _refs[i] = j.Allocate<Form>().This;
        }
    }

    private readonly Reference[] _refs = new Reference[10000];

    [Benchmark]
    public Object ResolveAny()
    {
        return JvmContext.Jvm!.ResolveObject(_refs[Random.Shared.Next(0, _refs.Length)]);
    }

    [Benchmark]
    public Form ResolveGeneric()
    {
        return JvmContext.Jvm!.Resolve<Form>(_refs[Random.Shared.Next(0, _refs.Length)]);
    }

    [Benchmark]
    public Object ResolveAnyEx()
    {
        return _refs[Random.Shared.Next(0, _refs.Length)].AsObject();
    }

    [Benchmark]
    public Form ResolveGenericEx()
    {
        return _refs[Random.Shared.Next(0, _refs.Length)].As<Form>();
    }
}