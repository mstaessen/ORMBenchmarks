using FluentNHibernate.Mapping;
using ORMBenchmarks.Model;

namespace ORMBenchmarks.Orms.NHibernate {
    public class OrderLineMap : ClassMap<OrderLine>
    {
        public OrderLineMap()
        {
            Table("`OrderLine`");
            CompositeId()
                .KeyProperty(x => x.OrderId, "OrderId")
                .KeyProperty(x => x.ProductId, "ProductId");
            Map(x => x.ProductName).Not.Nullable().Length(255);
            Map(x => x.Count).Not.Nullable();
            Map(x => x.UnitPrice).Not.Nullable().Precision(18).Scale(2);
        }
    }
}