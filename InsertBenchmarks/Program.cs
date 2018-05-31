using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;
using Bogus;

namespace InsertBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<InsertBenchmark>();

//            var benchmark = new InsertBenchmark {
//                Count = 10
//            };
//            benchmark.GlobalSetup();
        }
    }
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
    public class InsertBenchmark
    {
        private readonly Faker<Customer> customerFaker;
        private static readonly string DatabaseName = "AdoNetBenchmarks";
        private static readonly string ServerConncetionString = @"Server=(localdb)\mssqllocaldb";
        private static readonly string DatabaseConnectionString = $@"{ServerConncetionString};Database={DatabaseName}";

        private IList<Customer> customers;

        public InsertBenchmark()
        {
            customerFaker = new Faker<Customer>()
                .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                .RuleFor(x => x.LastName, f => f.Name.LastName())
                .RuleFor(x => x.Email, (f, c) => f.Internet.Email(c.FirstName, c.LastName))
                .RuleFor(x => x.Address, f => f.Address.FullAddress());
        }

//        [Params(10, 100)]
        public int Count { get; set; } = 100;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using (var sqlConnection = new SqlConnection(ServerConncetionString)) {
                sqlConnection.Open();
                sqlConnection.ExecuteNonQuery($@"DROP DATABASE IF EXISTS [{DatabaseName}]");
                sqlConnection.ExecuteNonQuery($@"CREATE DATABASE [{DatabaseName}]");
                sqlConnection.Close();
            }

            customers = customerFaker.Generate(Count);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            using (var sqlConnection = new SqlConnection(DatabaseConnectionString)) {
                sqlConnection.Open();
                sqlConnection.ExecuteNonQuery("DROP TABLE IF EXISTS [Customer]");
                sqlConnection.ExecuteNonQuery(@"CREATE TABLE [Customer](
    [Id] bigint not null identity(1, 1) PRIMARY KEY,
    [FirstName] nvarchar(127) not null,
    [LastName] nvarchar(127) not null,
    [Email] nvarchar(255) not null,
    [Address] nvarchar(255) not null
)"
                );
                sqlConnection.Close();
            }
        }

        [Benchmark(Description = "Multiple roundtrips, one value tuple per query")]
        public void MultipleRountripsWithSingleValue()
        {
            using (var sqlConnection = new SqlConnection(DatabaseConnectionString)) {
                sqlConnection.Open();
                foreach (var customer in customers) {
                    sqlConnection.ExecuteNonQuery("INSERT INTO [Customer] (FirstName, LastName, Email, Address) VALUES (@firstName, @lastName, @email, @address)",
                        new SqlParameter("firstName", customer.FirstName), new SqlParameter("lastName", customer.LastName), new SqlParameter("email", customer.Email),
                        new SqlParameter("address", customer.Address));
                }
            }
        }

        [Benchmark(Description = "Single roundtrip, multiple values in single statement.")]
        public void SingleInsertWithMultipleValues()
        {
            using (var sqlConnection = new SqlConnection(DatabaseConnectionString)) {
                sqlConnection.Open();
                sqlConnection.ExecuteNonQuery($@"INSERT INTO [Customer] (FirstName, LastName, Email, Address) 
                    VALUES {String.Join(", ", customers.Select((c, i) => $"(@firstName_{i}, @lastName_{i}, @email_{i}, @address_{i})"))}",
                    customers.Select((c, i) => new[] {
                        new SqlParameter($"firstName_{i}", c.FirstName),
                        new SqlParameter($"lastName_{i}", c.LastName),
                        new SqlParameter($"email_{i}", c.Email),
                        new SqlParameter($"address_{i}", c.Address)
                    }).SelectMany(x => x).ToArray());
            }
        }

        [Benchmark(Description = "Single roundtrip, multiple single value statements.")]
        public void MultipleInsertsInBatch()
        {
            using (var sqlConnection = new SqlConnection(DatabaseConnectionString)) {
                sqlConnection.Open();
                sqlConnection.ExecuteNonQuery(String.Join("; ", customers.Select((c, i) =>
                    $"INSERT INTO [Customer] (FirstName, LastName, Email, Address) VALUES (@firstName_{i}, @lastName_{i}, @email_{i}, @address_{i})")),
                    customers.Select((c, i) => new[] {
                        new SqlParameter($"firstName_{i}", c.FirstName),
                        new SqlParameter($"lastName_{i}", c.LastName),
                        new SqlParameter($"email_{i}", c.Email),
                        new SqlParameter($"address_{i}", c.Address)
                    }).SelectMany(x => x).ToArray());
            }
        }
    }

    internal class Customer
    {
        public long Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string Address { get; set; }
    }

    internal static class SqlConnectionExtensions
    {
        public static void ExecuteNonQuery(this SqlConnection connection, string commandText, params SqlParameter[] parameters)
        {
            using (var sqlCommand = new SqlCommand(commandText, connection)) {
                sqlCommand.Parameters.AddRange(parameters);
                sqlCommand.ExecuteNonQuery();
            }
        }
    }
}