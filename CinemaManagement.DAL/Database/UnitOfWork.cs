using System.Data;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbConnection _connection;
        private IDbTransaction _transaction;

        public UnitOfWork(SqliteConnectionFactory factory)
        {
            _connection = factory.CreateConnection();
            _connection.Open();
        }

        public void BeginTransaction() => _transaction = _connection.BeginTransaction();
        public void Commit() => _transaction?.Commit();
        public void Rollback() => _transaction?.Rollback();

        public void Dispose()
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }
    }
}