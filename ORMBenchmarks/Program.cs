using BenchmarkDotNet.Running;

namespace ORMBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<InsertBenchmarks>();

//            foreach (var orm in new[] {Orm.NHibernate, Orm.EntityFramework, Orm.EntityFrameworkCore}) {
//                var b = new Benchmarks {
//                    Orm = orm
//                };
//                b.GlobalSetup();
//                b.IterationSetup();
//                b.InsertSingleEntityAggregate();
//                b.InsertMultiEntityAggregate();
//            }
        }
    }
}