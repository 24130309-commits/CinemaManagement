using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public partial interface IUserRepository : IRepository<User>
{
    User? GetByUsername(string username);

    bool UpdatePassword(int id, string newHash);

    bool SetActive(int id, bool isActive);
}