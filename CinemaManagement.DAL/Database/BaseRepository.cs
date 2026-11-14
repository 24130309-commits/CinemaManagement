using Dapper;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL.Database;

public abstract class BaseRepository<T> : IRepository<T>
    where T : class
{
    protected readonly SqliteConnectionFactory ConnectionFactory;
    protected readonly string TableName;

    protected BaseRepository(
        SqliteConnectionFactory connectionFactory,
        string tableName)
    {
        ConnectionFactory = connectionFactory;
        TableName = tableName;
    }

    public virtual T? GetById(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.QuerySingleOrDefault<T>(
            $"SELECT * FROM [{TableName}] WHERE Id = @id",
            new { id });
    }

    public virtual IEnumerable<T> GetAll()
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<T>(
            $"SELECT * FROM [{TableName}] ORDER BY Id DESC").AsList();
    }

    public virtual int Insert(T entity)
    {
        throw new NotSupportedException(
            $"Repository {GetType().Name} phải định nghĩa câu lệnh INSERT cụ thể.");
    }

    public virtual bool Update(T entity)
    {
        throw new NotSupportedException(
            $"Repository {GetType().Name} phải định nghĩa câu lệnh UPDATE cụ thể.");
    }

    public virtual bool Delete(int id)
    {
        throw new NotSupportedException(
            $"Repository {GetType().Name} phải định nghĩa quy tắc DELETE cụ thể.");
    }
}