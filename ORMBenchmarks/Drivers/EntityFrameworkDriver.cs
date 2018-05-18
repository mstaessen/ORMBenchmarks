using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ORMBenchmarks.Orms.EF;

namespace ORMBenchmarks.Drivers
{
    public class EntityFrameworkDriver : IDriver
    {
        public void InsertOne<T>(T entity)
            where T : class
        {
            using (var context = new EntityFrameworkDbContext()) {
                context.Set<T>().Add(entity);
                context.SaveChanges();
            }
        }

        public void InserMany<T>(IEnumerable<T> entities)
            where T : class
        {
            using (var context = new EntityFrameworkDbContext()) {
                context.Set<T>().AddRange(entities);
                context.SaveChanges();
            }
        }

        public T QueryOne<T>(Expression<Func<T, bool>> filter)
            where T : class
        {
            using (var context = new EntityFrameworkDbContext()) {
                return context.Set<T>().SingleOrDefault(filter);
            }
        }

        public IReadOnlyList<T> FindMany<T>(Expression<Func<T, bool>> filter)
            where T : class
        {
            using (var context = new EntityFrameworkDbContext()) {
                return context.Set<T>().Where(filter).ToList();
            }
        }

        public override string ToString()
        {
            return "EntityFramework";
        }

        public void Dispose() { }
    }
}