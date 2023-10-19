// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using MahoTrans.Benchmarks;

int i = 0;

BenchmarkRunner.Run<DictsBenchs>();