using Microsoft.Data.Sqlite;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL.Database;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SqliteConnection _connection;
    private SqliteTransaction? _transaction;
    private bool _completed;

    public SqliteConnection Connection => _connection;

    public SqliteTransaction? Transaction => _transaction;

    public UnitOfWork(SqliteConnectionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _connection = factory.CreateConnection();
    }

    public void Begin()
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException(
                "Transaction đã được mở.");
        }

        _transaction = _connection.BeginTransaction();
        _completed = false;
    }

    public void BeginTransaction()
    {
        Begin();
    }

    public void Commit()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException(
                "Chưa mở transaction.");
        }

        _transaction.Commit();
        _completed = true;
        _transaction.Dispose();
        _transaction = null;
    }

    public void Rollback()
    {
        if (_transaction == null)
        {
            return;
        }

        _transaction.Rollback();
        _completed = true;
        _transaction.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        if (!_completed)
        {
            try
            {
                Rollback();
            }
            catch
            {
            }
        }

        _transaction?.Dispose();
        _connection.Dispose();
    }
}