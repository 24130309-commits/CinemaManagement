using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IUserService
{
    IEnumerable<User> GetAllUsers();

    User GetUserById(int id);

    void CreateUser(User user, string plainPassword);

    void UpdateUser(User user);

    void ResetPassword(int userId, string newPassword);

    void ToggleUserStatus(int userId);
}