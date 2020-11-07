using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelExecutionOnCollections.ConsoleApp
{
    public static class TaskEx
    {
        public static async Task<double> ForEachAsyncWithExceptions<T>(this IEnumerable<T> source, int dop, Func<T, Task<double>> body)
        {
            var tasks = Partitioner.Create(source)
                .GetPartitions(dop)
                .Select(partition => Task.Run(async () => 
                {
                    var acc = 0d;
                    using (partition)
                    {
                        while (partition.MoveNext())
                        {
                            acc += await body(partition.Current);
                        }
                    }

                    return acc;
                }));

            var results = await Task.WhenAll(tasks);

            return results.Aggregate(0d, (i, d) => i + d);
        }
    }
}