using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace ParallelExecutionOnCollections.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<BenchmarkParallels>();
        }
    }
    
    
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class BenchmarkParallels
    {
        private IImmutableList<int> _inputData;

        [Params(50)]
        public int N;

        private int degreeOfParalellism = 4;

        private readonly double salt = new Random().NextDouble(); 

        [GlobalSetup]
        public void Setup()
        {
            _inputData = Enumerable.Range(1, N).ToImmutableList();
        }

        [Benchmark]
        public async ValueTask<double> PlainValueTaskAsync()
        {
            var summ = 0d;
            foreach (var i in _inputData)
            {
                summ += await CalculateValueTaskAsync(i);
            }
            return summ;
        }

        [Benchmark]
        public async Task<double> PlainTaskAsync()
        {
            var summ = 0d;
            foreach (var i in _inputData)
            {
                summ += await CalculateTaskAsync(i);
            }
            return summ;
        }

        [Benchmark]
        public double PlainForeach()
        {
            var summ = 0d;
            foreach (var i in _inputData)
            {
                summ += Salt(i);
            }
            return summ;
        }

        [Benchmark]
        public double PlainFor()
        {
            var summ = 0d;
            for (var i = 0; i < _inputData.Count; i++)
            {
                summ += Salt(i);
            }
            return summ;
        }

        [Benchmark]
        public async ValueTask<double> PlainPartitionerAsync()
        {
            return  await _inputData.ForEachAsyncWithExceptions(1, CalculateTaskAsync);
        }
        
        [Benchmark]
        public async ValueTask<double> PartitionerAsync()
        {
           return  await _inputData.ForEachAsyncWithExceptions(degreeOfParalellism, CalculateTaskAsync);
        }

        [Benchmark]
        public async Task<double> SemaphoreAsync()
        {
            var semaphore = new SemaphoreSlim(degreeOfParalellism);
            
            var summ = 0d;
            foreach (var i in _inputData)
            {
                await semaphore.WaitAsync();
                CalculateTaskAsync(i)
                   .ContinueWith((t) =>
                   {
                       var result = t.Result;
                       Interlocked.Exchange(ref summ, summ + result);
                       return semaphore.Release();
                   });
            }
            return summ;
        }

        
        private async Task<double> CalculateTaskAsync(int i)
        {
            await Task.Delay(1);
            return Salt(i);
        }

        private async ValueTask<double> CalculateValueTaskAsync(int i)
        {
            await Task.Delay(1);
            return Salt(i);
        }

        double Salt(int i)
        {
            return i * salt;
        }
    }
}