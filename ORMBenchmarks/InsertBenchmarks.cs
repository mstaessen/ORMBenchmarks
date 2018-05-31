using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using ORMBenchmarks.Drivers;
using ORMBenchmarks.Model;

namespace ORMBenchmarks
{
    public class Config : ManualConfig
    {
        public Config()
        {
            Add(MarkdownExporter.Default);
            Add(CsvExporter.Default);
            Add(RPlotExporter.Default);
        }
    }

    [Config(typeof(Config))]
    [ClrJob]
    [MemoryDiagnoser]
    [MarkdownExporter, CsvExporter, RPlotExporter]
    public class InsertBenchmarks
    {
        private readonly DriverFactory driverFactory = new DriverFactory();

        private Random random;
        private Product[] products;
        private Order[] orders;

        private IDriver driver;

        private long randomProductId;
        private long randomOrderId;

        [Params(
            Orm.NHibernate, 
            Orm.EntityFramework, 
            Orm.EntityFrameworkCore
        )]
        public Orm Orm { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            random = new Random();
            products = Enumerable.Range(1, 100).Select(GenerateProduct).ToArray();
            orders = Enumerable.Range(1, 100).Select(GenerateOrder).ToArray();

            using (var sqlConnection = new SqlConnection(DatabaseConfig.MasterConnectionString)) {
                sqlConnection.Open();
                sqlConnection.ExecuteNonQuery($"DROP DATABASE IF EXISTS {DatabaseConfig.DatabaseName}");
                sqlConnection.ExecuteNonQuery($"CREATE DATABASE {DatabaseConfig.DatabaseName}");
                sqlConnection.Close();
            }

            using (var sqlConnection = new SqlConnection(DatabaseConfig.BenchmarkConnectionString)) {
                sqlConnection.Open();
                sqlConnection.ExecuteNonQuery(@"CREATE TABLE [Product] (
                    Id bigint NOT NULL IDENTITY (1, 1) PRIMARY KEY,
	                Name nvarchar(255) NOT NULL,
	                Price decimal(18, 2) NOT NULL
                )");
                sqlConnection.ExecuteNonQuery(@"CREATE TABLE [Order] (
                    Id bigint NOT NULL IDENTITY (1, 1) PRIMARY KEY,
	                ClientId bigint NOT NULL,
	                ShippingAddress_StreetName nvarchar(255) NOT NULL,
	                ShippingAddress_StreetNumber nvarchar(15) NOT NULL,
	                ShippingAddress_PostalCode nvarchar(15) NOT NULL,
	                ShippingAddress_City nvarchar(127) NOT NULL,
	                ShippingAddress_Country nvarchar(127) NOT NULL
                )");
                sqlConnection.ExecuteNonQuery(@"CREATE TABLE [OrderLine] (
                    OrderId bigint NOT NULL,
	                ProductId bigint NOT NULL,
	                ProductName nvarchar(255) NOT NULL,
	                Count int NOT NULL,
	                UnitPrice decimal(18, 2) NOT NULL,
                    PRIMARY KEY(OrderId, ProductId),
                    FOREIGN KEY(OrderId) REFERENCES [Order](Id) ON DELETE CASCADE,
                    FOREIGN KEY(ProductId) REFERENCES [Product](Id),
                )");

                sqlConnection.ExecuteNonQuery($"INSERT INTO [Product] (Name, Price) VALUES {String.Join(", ", products.Select(p => $"('{p.Name}', {p.Price})"))}");
                foreach (var o in orders) {
                    sqlConnection.ExecuteNonQuery($@"INSERT INTO [Order] (ClientId, ShippingAddress_StreetName, ShippingAddress_StreetNumber, ShippingAddress_PostalCode, ShippingAddress_City, ShippingAddress_Country) 
                        VALUES ('{o.ClientId}', '{o.ShippingAddress.StreetName}', '{o.ShippingAddress.StreetNumber}', '{o.ShippingAddress.PostalCode}', '{o.ShippingAddress.City}', '{o.ShippingAddress.Country}')");
                    sqlConnection.ExecuteNonQuery($@"INSERT INTO [OrderLine] (OrderId, ProductId, ProductName, Count, UnitPrice) 
                        VALUES {String.Join(", ", o.Lines.Select(l => $"(SCOPE_IDENTITY(), {l.ProductId}, '{l.ProductName}', {l.Count}, {l.UnitPrice})"))}");
                }
                sqlConnection.Close();
            }

            driver = driverFactory.Create(Orm);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            using (var sqlConnection = new SqlConnection(DatabaseConfig.BenchmarkConnectionString)) {
                sqlConnection.Open();
                // https://www.mssqltips.com/sqlservertip/1360/clearing-cache-for-sql-server-performance-testing/
                sqlConnection.ExecuteNonQuery("CHECKPOINT");
                sqlConnection.ExecuteNonQuery("DBCC DROPCLEANBUFFERS;");
                sqlConnection.Close();
            }
            randomProductId = random.Next(products.Length);
            randomOrderId = random.Next(orders.Length);
        }

        [Benchmark]
        public void InsertSingle()
        {
            driver.InsertOne(GenerateProduct(0));
        }

        [Benchmark]
        public void InsertMultiple()
        {
            driver.InsertOne(GenerateOrder(0));
        }

        [Benchmark]
        public Product QuerySingle()
        {
            return driver.QueryOne<Product>(p => p.Id == randomProductId);
        }

        [Benchmark]
        public Order QueryMultiple()
        {
            return driver.QueryOne<Order>(o => o.Id == randomOrderId);
        }

        [IterationCleanup]
        public void IterationCleanup() { }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            driver.Dispose();
        }

        internal Product GenerateProduct(int productId)
        {
            return new Product
            {
                Id = productId,
                Name = random.NextString(random.Next(20, 255)),
                Price = new decimal(Math.Round(random.NextDouble() * 10000, 2))
            };
        }

        internal Order GenerateOrder(int orderId)
        {
            var order = new Order
            {
                Id = orderId,
                ClientId = random.Next(20, 1024),
                ShippingAddress = GenerateAddress(),
                Lines = new List<OrderLine>()
            };
            while (order.Lines.Count < 5)
            {
                var product = products[random.Next(products.Length)];
                if (order.Lines.All(l => l.ProductId != product.Id))
                {
                    order.Lines.Add(new OrderLine
                    {
                        OrderId = orderId,
                        ProductId = product.Id,
                        Count = random.Next(1, 5),
                        ProductName = product.Name,
                        UnitPrice = product.Price
                    });
                }
            }
            return order;
        }

        internal Address GenerateAddress()
        {
            return new Address
            {
                StreetName = random.NextString(random.Next(20, 127)),
                StreetNumber = random.Next(1, 1024).ToString(),
                PostalCode = random.NextString(random.Next(4, 10)),
                City = random.NextString(random.Next(20, 127)),
                Country = random.NextString(random.Next(20, 127))
            };
        }
    }

    internal class DriverFactory
    {
        public IDriver Create(Orm orm)
        {
            switch (orm) {
                case Orm.NHibernate:
                    return new NHibernateDriver();
                case Orm.EntityFramework:
                    return new EntityFrameworkDriver();
                case Orm.EntityFrameworkCore:
                    return new EntityFrameworkCoreDriver();
                default:
                    throw new ArgumentOutOfRangeException(nameof(orm), orm, null);
            }
        }
    }

    internal static class SqlConnectionExtensions
    {
        public static void ExecuteNonQuery(this SqlConnection connection, string commandText)
        {
            using (var sqlCommand = new SqlCommand(commandText, connection)) {
                sqlCommand.ExecuteNonQuery();
            }
        }
    }

    internal static class RandomExtensions
    {
        public static string NextString(this Random random, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
    }
}