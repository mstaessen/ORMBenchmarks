namespace ORMBenchmarks.Model
{
    public class OrderLine
    {
        public virtual long OrderId { get; set; }

        public virtual long ProductId { get; set; }

        public virtual string ProductName { get; set; }

        public virtual int Count { get; set; }

        public virtual decimal UnitPrice { get; set; }

        public virtual decimal LineAmount => UnitPrice * Count;

        protected bool Equals(OrderLine other)
        {
            return OrderId == other.OrderId
                && ProductId == other.ProductId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != this.GetType()) {
                return false;
            }
            return Equals((OrderLine) obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                return (OrderId.GetHashCode() * 397) ^ ProductId.GetHashCode();
            }
        }
    }
}