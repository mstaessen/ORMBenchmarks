using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using ORMBenchmarks.Model;

namespace ORMBenchmarks.Orms.EF
{
    public class EntityFrameworkDbContext : DbContext
    {
        public DbSet<Order> Orders { get; set; }

        public DbSet<Product> Products { get; set; }

        public EntityFrameworkDbContext() : base(DatabaseConfig.BenchmarkConnectionString) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(b => {
                b.Property(x => x.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
                b.HasMany(x => x.Lines).WithRequired().HasForeignKey(x => x.OrderId);
            });

            modelBuilder.Entity<OrderLine>(b => {
                b.HasKey(x => new {x.OrderId, x.ProductId});
            });

            modelBuilder.Entity<Product>(b => {
                b.Property(x => x.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            });

            modelBuilder.ComplexType<Address>();
        }
    }

    internal static class ModelBuilderExtensions
    {
        internal static void Entity<TEntity>(this DbModelBuilder modelBuilder, Action<EntityTypeConfiguration<TEntity>> configuration)
            where TEntity : class
        {
            configuration(modelBuilder.Entity<TEntity>());
        }
    }
}