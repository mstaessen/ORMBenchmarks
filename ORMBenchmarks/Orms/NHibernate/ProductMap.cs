using FluentNHibernate.Mapping;
using ORMBenchmarks.Model;

namespace ORMBenchmarks.Orms.NHibernate
{
    public class ProductMap : ClassMap<Product>
    {
        public ProductMap()
        {
            Table("`Product`");
            Id(x => x.Id).Column("Id");
            Map(x => x.Name).Not.Nullable().Length(255);
            Map(x => x.Price).Not.Nullable().Precision(18).Scale(2);
        }
    }
}