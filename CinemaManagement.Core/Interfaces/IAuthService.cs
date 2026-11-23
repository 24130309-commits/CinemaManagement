using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IAuthService
{
    User Login(string username, string password);

    void Logout();

    void ChangePassword(
        int userId,
        string oldPassword,
        string newPassword);

    string HashPassword(string password);

    bool VerifyPassword(string password, string hash);
}