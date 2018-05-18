using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using ORMBenchmarks.Orms.NHibernate;

namespace ORMBenchmarks.Drivers
{
    public class NHibernateDriver : IDriver
    {
        private readonly ISessionFactory sessionFactory;

        public NHibernateDriver()
        {
            sessionFactory = Fluently
                .Configure()
                .Database(MsSqlConfiguration.MsSql2012.ConnectionString(DatabaseConfig.BenchmarkConnectionString))
                .Mappings(m => m.FluentMappings
                    .Add<ProductMap>()
                    .Add<OrderMap>()
                    .Add<OrderLineMap>())
                .ExposeConfiguration(configuration => new SchemaValidator(configuration).Validate())
                .BuildSessionFactory();
        }

        public void InsertOne<T>(T entity)
            where T : class
        {
            using (var session = sessionFactory.OpenSession()) {
                session.Save(entity);
                session.Flush();
            }
        }

        public void InserMany<T>(IEnumerable<T> entities)
            where T : class
        {
            using (var session = sessionFactory.OpenSession()) {
                foreach (var entity in entities) {
                    session.Save(entity);
                }
                session.Flush();
            }
        }

        public T QueryOne<T>(Expression<Func<T, bool>> filter)
            where T : class
        {
            using (var session = sessionFactory.OpenSession()) {
                return session.Query<T>().SingleOrDefault(filter);
            }
        }

        public IReadOnlyList<T> FindMany<T>(Expression<Func<T, bool>> filter)
            where T : class
        {
            using (var session = sessionFactory.OpenSession()) {
                return session.Query<T>().Where(filter).ToList();
            }
        }

        public override string ToString()
        {
            return "NHibernate";
        }

        public void Dispose()
        {
            sessionFactory?.Dispose();
        }
    }
}