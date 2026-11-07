namespace CinemaManagement.Core.Interfaces;

public interface IAuthService
{
    bool Login(string username, string password);
    void Logout();
    bool ChangePassword(int userId, string oldPassword, string newPassword);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}