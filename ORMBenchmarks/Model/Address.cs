namespace ORMBenchmarks.Model
{
    public class Address
    {
        public virtual string StreetName { get; set; }

        public virtual string StreetNumber { get; set; }

        public virtual string PostalCode { get; set; }

        public virtual string City { get; set; }

        public virtual string Country { get; set; }
    }
}