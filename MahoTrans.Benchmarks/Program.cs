// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace MahoTrans.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
            .AddJob(Job.Default.WithId(".NET 6").WithRuntime(CoreRuntime.Core60))
            .AddJob(Job.Default.WithId(".NET 8 w/o PGO").WithRuntime(CoreRuntime.Core80)
                .WithEnvironmentVariable("DOTNET_TieredPGO", "0"))
            .AddJob(Job.Default.WithId(".NET 8").WithRuntime(CoreRuntime.Core80));
        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, config);
    }
}
