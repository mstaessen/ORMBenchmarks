namespace ORMBenchmarks.Model
{
    public class Product
    {
        public virtual long Id { get; set; }

        public virtual string Name { get; set; }

        public virtual decimal Price { get; set; }
    }
}