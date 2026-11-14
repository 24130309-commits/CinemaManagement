using System.Collections.Generic;

namespace CinemaManagement.Core.Interfaces;

public interface IRepository<T> where T : class
{
    T? GetById(int id);
    IEnumerable<T> GetAll();
    int Insert(T entity);
    bool Update(T entity);
    bool Delete(int id);
}