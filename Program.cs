using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Running;

namespace IBANValidation
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Benchmark>(
                ManualConfig.Create(DefaultConfig.Instance)
                    .With(new MemoryDiagnoser()));
        }
    }
}
