using System.Collections.Generic;
using System.Linq;

namespace ORMBenchmarks.Model
{
    public class Order
    {
        public virtual long Id { get; set; }

        public virtual long ClientId { get; set; }

        public virtual Address ShippingAddress { get; set; }

        public virtual ICollection<OrderLine> Lines { get; set; }

        public virtual decimal TotalAmount => Lines.Sum(x => x.LineAmount);
    }
}