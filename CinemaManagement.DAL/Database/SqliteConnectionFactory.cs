// CinemaManagement.DAL/SqliteConnectionFactory.cs
using Microsoft.Data.Sqlite;
using CinemaManagement.Core.Constants;

namespace CinemaManagement.DAL
{
    public class SqliteConnectionFactory
    {
        private readonly string _connectionString;

        public SqliteConnectionFactory(string connectionString = AppConstants.ConnectionString)
        {
            _connectionString = connectionString;
        }

        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }
    }
}

