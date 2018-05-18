using Microsoft.EntityFrameworkCore;
using ORMBenchmarks.Model;

namespace ORMBenchmarks.Orms.EFCore
{
    public class EntityFrameworkCoreDbContext : DbContext
    {
        public DbSet<Order> Orders { get; set; }

        public DbSet<Product> Products { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(DatabaseConfig.BenchmarkConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(b => {
                b.ToTable("Product");
                b.Property(x => x.Id).ValueGeneratedOnAdd().UseSqlServerIdentityColumn();
            });

            modelBuilder.Entity<Order>(b => {
                b.ToTable("Order");
                b.Property(x => x.Id).ValueGeneratedOnAdd().UseSqlServerIdentityColumn();
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId);
                b.OwnsOne(x => x.ShippingAddress);
            });

            modelBuilder.Entity<OrderLine>(b => {
                b.ToTable("OrderLine");
                b.HasKey(x => new {x.OrderId, x.ProductId});
            });
        }
    }
}