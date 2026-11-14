using System;
using System.IO;

namespace CinemaManagement.DAL.Database
{
    public class DbInitializer
    {
        private readonly SqliteConnectionFactory _connectionFactory;

        public DbInitializer(SqliteConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void Initialize()
        {
            string schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "Schema.sql");
            if (!File.Exists(schemaPath))
                schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schema.sql");

            if (!File.Exists(schemaPath))
                throw new FileNotFoundException("Không tìm thấy file Schema.sql trong thư mục build.");

            string sql = File.ReadAllText(schemaPath);
            using var conn = _connectionFactory.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}