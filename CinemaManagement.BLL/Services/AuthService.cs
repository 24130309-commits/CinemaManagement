using System;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IAuditService _auditService;

        public AuthService(IUserRepository userRepo, IAuditService auditService = null)
        {
            _userRepo = userRepo;
            _auditService = auditService;
        }

        public User Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                throw new BusinessException("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");

            var user = _userRepo.GetByUsername(username.Trim());
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new BusinessException("Tên đăng nhập hoặc mật khẩu không chính xác.");

            if (!user.IsActive)
                throw new BusinessException("Tài khoản đã bị khóa. Vui lòng liên hệ Quản trị viên.");

            CurrentUserSession.SetUser(user);
            _auditService?.Log("LOGIN", "User", user.Id, $"Người dùng {user.Username} đăng nhập hệ thống.");
            return user;
        }

        public void Logout()
        {
            if (CurrentUserSession.IsLoggedIn)
            {
                var u = CurrentUserSession.CurrentUser;
                _auditService?.Log("LOGOUT", "User", u.Id, $"Người dùng {u.Username} đăng xuất.");
                CurrentUserSession.Clear();
            }
        }

        public void ChangePassword(int userId, string oldPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                throw new BusinessException("Mật khẩu mới phải có ít nhất 6 ký tự.");

            var user = _userRepo.GetById(userId) ?? throw new BusinessException("Không tìm thấy tài khoản.");
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
                throw new BusinessException("Mật khẩu hiện tại không đúng.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _userRepo.Update(user);
            _auditService?.Log("CHANGE_PASSWORD", "User", user.Id, "Đổi mật khẩu cá nhân.");
        }
    }
}