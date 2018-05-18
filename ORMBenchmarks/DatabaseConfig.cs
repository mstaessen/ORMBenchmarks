namespace ORMBenchmarks
{
    public class DatabaseConfig
    {
        internal static readonly string DatabaseName = "ORMBenchmarks";
        internal static readonly string MasterDatabaseName = "master";
        internal static readonly string ServerConnectionString = @"Server=(localdb)\mssqllocaldb;Integrated Security=true;";
        internal static readonly string MasterConnectionString = $"{ServerConnectionString}Database={MasterDatabaseName};";
        internal static readonly string BenchmarkConnectionString = $"{ServerConnectionString}Database={DatabaseName};";
    }
}