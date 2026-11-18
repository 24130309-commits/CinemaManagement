using System.Collections.Concurrent;
using CinemaManagement.BLL.Session;
using CinemaManagement.BLL.Validators;
using CinemaManagement.Core.Constants;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public sealed class AuthService : IAuthService
{
    private sealed record LoginFailure(int Count, DateTime? LockedUntil);

    private readonly IUserRepository _users;
    private readonly CurrentUserSession _session;
    private readonly UserValidator _validator;

    private readonly ConcurrentDictionary<string, LoginFailure> _failures =
        new(StringComparer.OrdinalIgnoreCase);

    public AuthService(
        IUserRepository users,
        CurrentUserSession session,
        UserValidator validator)
    {
        _users = users;
        _session = session;
        _validator = validator;
    }

    public bool Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password))
            return false;

        var key = username.Trim();

        if (_failures.TryGetValue(key, out var state) &&
            state.LockedUntil > DateTime.Now)
        {
            throw new BusinessException(
                $"Tài khoản tạm khóa đến {state.LockedUntil:HH:mm:ss} do đăng nhập sai nhiều lần.");
        }

        var user = _users.GetByUsername(key);

        if (user is null ||
            !user.IsActive ||
            !VerifyPassword(password, user.PasswordHash))
        {
            var count = (state?.Count ?? 0) + 1;

            DateTime? lockedUntil =
                count >= AppConstants.MaxFailedLoginAttempts
                    ? DateTime.Now.AddMinutes(AppConstants.LoginLockoutMinutes)
                    : null;

            _failures[key] = new LoginFailure(
                lockedUntil is null ? count : 0,
                lockedUntil);

            if (lockedUntil.HasValue)
            {
                throw new BusinessException(
                    $"Bạn đã nhập sai {AppConstants.MaxFailedLoginAttempts} lần. Tài khoản tạm khóa {AppConstants.LoginLockoutMinutes} phút.");
            }

            return false;
        }

        _failures.TryRemove(key, out _);
        _session.Set(user);

        return true;
    }

    public void Logout()
    {
        _session.Clear();
    }

    public bool ChangePassword(
        int userId,
        string oldPassword,
        string newPassword)
    {
        var user = _users.GetById(userId);

        if (user is null ||
            !VerifyPassword(oldPassword, user.PasswordHash))
            return false;

        _validator.ValidatePassword(newPassword);

        if (VerifyPassword(newPassword, user.PasswordHash))
        {
            throw new BusinessException(
                "Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        return _users.UpdatePassword(
            userId,
            HashPassword(newPassword));
    }

    public string HashPassword(string password)
    {
        _validator.ValidatePassword(password);

        return global::BCrypt.Net.BCrypt.HashPassword(
            password,
            workFactor: 12);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(hash))
            return false;

        try
        {
            return global::BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception)
        {
            return false;
        }
    }
}