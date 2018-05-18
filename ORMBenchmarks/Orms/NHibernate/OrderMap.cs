using FluentNHibernate.Mapping;
using ORMBenchmarks.Model;

namespace ORMBenchmarks.Orms.NHibernate {
    public class OrderMap : ClassMap<Order>
    {
        public OrderMap()
        {
            Table("`Order`");
            Id(x => x.Id).Column("Id");
            Map(x => x.ClientId).Column("ClientId").Not.Nullable();
            Component(x => x.ShippingAddress, m => {
                m.Map(x => x.StreetName).Column("ShippingAddress_StreetName").Not.Nullable().Length(255);
                m.Map(x => x.StreetNumber).Column("ShippingAddress_StreetNumber").Not.Nullable().Length(15);
                m.Map(x => x.PostalCode).Column("ShippingAddress_PostalCode").Not.Nullable().Length(15);
                m.Map(x => x.City).Column("ShippingAddress_City").Not.Nullable().Length(127);
                m.Map(x => x.Country).Column("ShippingAddress_Country").Not.Nullable().Length(127);
            });
            HasMany(x => x.Lines).KeyColumn("OrderId").Inverse().Cascade.Delete();
        }
    }
}