using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IUserService
{
    IEnumerable<User> GetAll();
    void Create(User user);
    void Update(User user);
    void ResetPassword(int userId, string newPassword);
    void SetActive(int userId, bool isActive);
}