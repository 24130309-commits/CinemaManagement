using Microsoft.Data.Sqlite;

namespace CinemaManagement.DAL.Database;

public sealed class UnitOfWork : IDisposable
{
    private readonly SqliteConnection _connection;
    private SqliteTransaction? _transaction;
    private bool _completed;

    public UnitOfWork(SqliteConnectionFactory factory)
    {
        _connection = factory.CreateConnection();
    }

    public SqliteConnection Connection => _connection;

    public SqliteTransaction? Transaction => _transaction;

    public void Begin()
    {
        if (_transaction != null)
            throw new InvalidOperationException("Transaction đã được mở.");

        _transaction = _connection.BeginTransaction();
        _completed = false;
    }

    public void Commit()
    {
        if (_transaction == null)
            throw new InvalidOperationException("Chưa mở transaction.");

        _transaction.Commit();
        _completed = true;
        _transaction.Dispose();
        _transaction = null;
    }

    public void Rollback()
    {
        if (_transaction == null)
            return;

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
                // Giữ nguyên lỗi gốc.
            }
        }

        _transaction?.Dispose();
        _connection.Dispose();
    }
}