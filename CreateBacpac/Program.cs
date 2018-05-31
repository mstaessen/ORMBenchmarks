using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using ORMBenchmarks.Model;

namespace CreateBacpac
{
    public class Program
    {
        private static readonly Random Random = new Random();

        private const int ProductCount = 100;
        private const int OrderCount = 1000;

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            GenFu.GenFu.Configure<Product>()
                .Fill(p => p.Price, () => new decimal(Math.Round(Random.NextDouble() * 1000, 2)));

            var products = Enumerable.Range(1, ProductCount).Select(GenerateProduct).ToArray();
            var orders = Enumerable.Range(1, OrderCount).Select(orderId => GenerateOrder(orderId, products)).ToArray();

            using (var sqlConnection = new SqlConnection(DatabaseConfig.MasterConnectionString)) {
                sqlConnection.Open();
                sqlConnection.ExecuteNonQuery($"IF EXISTS(select * from sys.databases where name = '{DatabaseConfig.DatabaseName}') DROP DATABASE {DatabaseConfig.DatabaseName}");
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

                var productTable = new DataTable();
                productTable.Columns.Add(nameof(Product.Name));
                productTable.Columns.Add(nameof(Product.Price));
                foreach (var p in products) {
                    var row = productTable.NewRow();
                    row[nameof(Product.Name)] = p.Name;
                    row[nameof(Product.Price)] = p.Price;
                    productTable.Rows.Add(row);
                }
                using (var bulkCopy = new SqlBulkCopy(sqlConnection)) {
                    bulkCopy.DestinationTableName = nameof(Product);
                    bulkCopy.WriteToServer(productTable);
                }

                foreach (var o in orders) {
                    var orderId = sqlConnection.ExecuteScalar<long>(
                        @"INSERT INTO [Order] (
                            ClientId, 
                            ShippingAddress_StreetName, 
                            ShippingAddress_StreetNumber, 
                            ShippingAddress_PostalCode, 
                            ShippingAddress_City, 
                            ShippingAddress_Country
                        ) VALUES (@p1, @p2, @p3, @p4, @p5, @p6); 
                        SELECT SCOPE_IDENTITY();",
                        o.ClientId,
                        o.ShippingAddress.StreetName,
                        o.ShippingAddress.StreetNumber,
                        o.ShippingAddress.PostalCode,
                        o.ShippingAddress.City,
                        o.ShippingAddress.Country
                    );
                    var orderLinesTable = new DataTable();
                    orderLinesTable.Columns.Add(nameof(OrderLine.OrderId));
                    orderLinesTable.Columns.Add(nameof(OrderLine.ProductId));
                    orderLinesTable.Columns.Add(nameof(OrderLine.ProductName));
                    orderLinesTable.Columns.Add(nameof(OrderLine.Count));
                    orderLinesTable.Columns.Add(nameof(OrderLine.UnitPrice));
                    foreach (var l in o.Lines) {
                        var row = orderLinesTable.NewRow();
                        row[nameof(OrderLine.OrderId)] = orderId;
                        row[nameof(OrderLine.ProductId)] = l.ProductId;
                        row[nameof(OrderLine.ProductName)] = l.ProductName;
                        row[nameof(OrderLine.Count)] = l.Count;
                        row[nameof(OrderLine.UnitPrice)] = l.UnitPrice;
                        orderLinesTable.Rows.Add(row);
                    }
                    using (var bulkCopy = new SqlBulkCopy(sqlConnection)) {
                        bulkCopy.DestinationTableName = nameof(OrderLine);
                        bulkCopy.WriteToServer(orderLinesTable);
                    }
                }
                sqlConnection.Close();
            }
        }

        internal static Product GenerateProduct(int productId)
        {
            return GenFu.GenFu.New<Product>();
        }

        internal static Order GenerateOrder(int orderId, Product[] products)
        {
            var order = new Order {
                Id = orderId,
                ClientId = Random.Next(1, 1024),
                ShippingAddress = GenerateAddress(),
                Lines = new List<OrderLine>()
            };
            while (order.Lines.Count < 5) {
                var product = products[Random.Next(products.Length)];
                if (order.Lines.All(l => l.ProductId != product.Id)) {
                    order.Lines.Add(new OrderLine {
                        OrderId = orderId,
                        ProductId = product.Id,
                        Count = Random.Next(1, 5),
                        ProductName = product.Name,
                        UnitPrice = product.Price
                    });
                }
            }
            return order;
        }

        internal static Address GenerateAddress()
        {
            return GenFu.GenFu.New<Address>();
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

        public static T ExecuteScalar<T>(this SqlConnection connection, string commandText, params object[] parameters)
        {
            using (var sqlCommand = new SqlCommand(commandText, connection)) {
                sqlCommand.Parameters.AddRange(parameters);
                return (T) sqlCommand.ExecuteScalar();
            }
        }
    }
}