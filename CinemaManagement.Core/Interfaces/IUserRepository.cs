using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    User? GetByUsername(string username);
    bool UpdatePassword(int id, string newHash);
    bool SetActive(int id, bool isActive);
}