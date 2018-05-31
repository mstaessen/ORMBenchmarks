using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using BenchmarkDotNet.Attributes.Columns;

namespace SqlBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<InsertBenchmarks>();

            // var benchmark = new InsertBenchmarks {
            // Count = 10
            // };
            // benchmark.GlobalSetup();
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
    [RankColumn]
    [MemoryDiagnoser]
    [MarkdownExporter, CsvExporter, RPlotExporter]
    public class InsertBenchmarks
    {
        private static readonly string DatabaseName = "SqlBenchmarks";
        private static readonly string ServerConncetionString = @"Server=(localdb)\mssqllocaldb";
        private static readonly string DatabaseConnectionString = $@"{ServerConncetionString};Database={DatabaseName}";
        private IList<Person> people;

        [Params(10000)]
        public int Count { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            using (var sqlConnection = new SqlConnection(ServerConncetionString)) {
                sqlConnection.Open();
                sqlConnection.ExecuteNonQuery($@"IF EXISTS(select * from sys.databases where name='{DatabaseName}') DROP DATABASE {DatabaseName}");
                sqlConnection.ExecuteNonQuery($@"CREATE DATABASE [{DatabaseName}]");
                sqlConnection.Close();
            }

            people = Enumerable.Range(1, Count)
                .Select(_ => new Bogus.Person())
                .Select(p => new Person {
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    BirthDate = p.DateOfBirth,
                    Address = $"{p.Address.Street}, {p.Address.ZipCode}, {p.Address.City}",
                    Email = p.Email
                })
                .ToList();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            using (var sqlConnection = new SqlConnection(DatabaseConnectionString)) {
                sqlConnection.Open();
                sqlConnection.ExecuteNonQuery("IF OBJECT_ID('Person', 'U') IS NOT NULL DROP TABLE [Person]");
                sqlConnection.ExecuteNonQuery(@"CREATE TABLE [Person](
    [Id] bigint not null identity(1, 1) PRIMARY KEY,
    [FirstName] nvarchar(127) not null,
    [LastName] nvarchar(127) not null,
    [BirthDate] datetime2 not null,
    [Email] nvarchar(255) not null,
    [Address] nvarchar(255) not null
)"
                );
                sqlConnection.Close();
            }
        }

        [Benchmark(Baseline = true)]
        public void OneQueryOneValue()
        {
            using (var sqlConnection = new SqlConnection(DatabaseConnectionString)) {
                sqlConnection.Open();
                foreach (var person in people) {
                    sqlConnection.ExecuteNonQuery("INSERT INTO [Person] (FirstName, LastName, BirthDate, Email, Address) VALUES (@firstName, @lastName, @birthDate, @email, @address)",
                        new SqlParameter("firstName", person.FirstName), new SqlParameter("lastName", person.LastName), new SqlParameter("birthDate", person.BirthDate),
                        new SqlParameter("email", person.Email), new SqlParameter("address", person.Address));
                }
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        public void OneQueryNValues(int batchSize)
        {
            using (var sqlConnection = new SqlConnection(DatabaseConnectionString)) {
                sqlConnection.Open();
                foreach (var bucket in people.Bucketize(batchSize)) {
                    var bucketArray = bucket.ToArray();
                    sqlConnection.ExecuteNonQuery($@"INSERT INTO [Person] (FirstName, LastName, BirthDate, Email, Address) 
                    VALUES {String.Join(", ", bucketArray.Select((c, i) => $"(@firstName_{i}, @lastName_{i}, @birthDate_{i}, @email_{i}, @address_{i})"))}",
                        bucketArray.Select((c, i) => new[] {
                            new SqlParameter($"firstName_{i}", c.FirstName),
                            new SqlParameter($"lastName_{i}", c.LastName),
                            new SqlParameter($"birthDate_{i}", c.BirthDate),
                            new SqlParameter($"email_{i}", c.Email),
                            new SqlParameter($"address_{i}", c.Address)
                        }).SelectMany(x => x).ToArray());
                }
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public void NQueriesOneValue(int batchSize)
        {
            using (var sqlConnection = new SqlConnection(DatabaseConnectionString)) {
                sqlConnection.Open();
                foreach (var bucket in people.Bucketize(batchSize)) {
                    var bucketArray = bucket.ToArray();
                    sqlConnection.ExecuteNonQuery(String.Join("; ", bucketArray.Select((c, i) =>
                            $"INSERT INTO [Person] (FirstName, LastName, BirthDate, Email, Address) VALUES (@firstName_{i}, @lastName_{i}, @birthDate_{i}, @email_{i}, @address_{i})")),
                        bucketArray.Select((c, i) => new[] {
                            new SqlParameter($"firstName_{i}", c.FirstName),
                            new SqlParameter($"lastName_{i}", c.LastName),
                            new SqlParameter($"birthDate_{i}", c.BirthDate),
                            new SqlParameter($"email_{i}", c.Email),
                            new SqlParameter($"address_{i}", c.Address)
                        }).SelectMany(x => x).ToArray());
                }
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public void SqlBulkCopy(int batchSize)
        {
            var dt = new DataTable();
            dt.Columns.Add("FirstName", typeof(string));
            dt.Columns.Add("LastName", typeof(string));
            dt.Columns.Add("BirthDate", typeof(DateTime));
            dt.Columns.Add("Email", typeof(string));
            dt.Columns.Add("Address", typeof(string));

            foreach (var person in people) {
                var row = dt.NewRow();
                row["FirstName"] = person.FirstName;
                row["LastName"] = person.LastName;
                row["BirthDate"] = person.BirthDate;
                row["Email"] = person.Email;
                row["Address"] = person.Address;
                dt.Rows.Add(row);
            }

            using (var sqlConnection = new SqlConnection(DatabaseConnectionString)) {
                sqlConnection.Open();
                using (var sqlBulkCopy = new SqlBulkCopy(sqlConnection) {
                    BatchSize = batchSize,
                    DestinationTableName = "Person"
                }) {
                    sqlBulkCopy.WriteToServer(dt);
                }
            }
        }
    }

    internal class Person
    {
        public long Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public DateTime BirthDate { get; set; }

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

    internal static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Bucketize<T>(this IEnumerable<T> items, int bucketSize)
        {
            var enumerator = items.GetEnumerator();
            while (enumerator.MoveNext()) {
                yield return GetNextBucket(enumerator, bucketSize);
            }
        }

        private static IEnumerable<T> GetNextBucket<T>(IEnumerator<T> enumerator, int maxItems)
        {
            var count = 0;
            do {
                yield return enumerator.Current;

                count++;
                if (count == maxItems) {
                    yield break;
                }
            } while (enumerator.MoveNext());
        }
    }
}