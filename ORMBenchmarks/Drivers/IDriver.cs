using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ORMBenchmarks.Drivers
{
    public interface IDriver : IDisposable
    {
        void InsertOne<T>(T entity)
            where T : class;

        void InserMany<T>(IEnumerable<T> entities)
            where T : class;

        T QueryOne<T>(Expression<Func<T, bool>> filter)
            where T : class;

        IReadOnlyList<T> FindMany<T>(Expression<Func<T, bool>> filter)
            where T : class;
    }
}