using BenchmarkDotNet.Running;
using ManualMapping.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(MapBenchmarks).Assembly).Run(args);
