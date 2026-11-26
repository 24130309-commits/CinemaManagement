using System.Collections.Concurrent;
using System.Diagnostics;
using CinemaManagement.BLL.Session;
using CinemaManagement.BLL.Validators;
using CinemaManagement.Core.Constants;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public sealed class AuthService : IAuthService
{
    private sealed record LoginFailure(int Count, DateTime? LockedUntil);

    private readonly IUserRepository _users;
    private readonly CurrentUserSession _session;
    private readonly UserValidator _validator;
    private readonly IAuditService? _audit;

    private readonly ConcurrentDictionary<string, LoginFailure> _failures =
        new(StringComparer.OrdinalIgnoreCase);

    public AuthService(
        IUserRepository users,
        CurrentUserSession session,
        UserValidator validator,
        IAuditService? audit = null)
    {
        _users = users;
        _session = session;
        _validator = validator;
        _audit = audit;
    }

    public User Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password))
        {
            throw new BusinessException(
                "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
        }

        var key = username.Trim();

        _failures.TryGetValue(key, out var state);

        if (state?.LockedUntil is DateTime lockedUntil &&
            lockedUntil > DateTime.Now)
        {
            throw new BusinessException(
                $"Tài khoản tạm khóa đến {lockedUntil:HH:mm:ss} do đăng nhập sai nhiều lần.");
        }

        var user = _users.GetByUsername(key);

        if (user is null ||
            !user.IsActive ||
            !VerifyPassword(password, user.PasswordHash))
        {
            var count = state?.LockedUntil.HasValue == true
                ? 1
                : (state?.Count ?? 0) + 1;

            DateTime? newLockedUntil =
                count >= AppConstants.MaxFailedLoginAttempts
                    ? DateTime.Now.AddMinutes(
                        AppConstants.LoginLockoutMinutes)
                    : null;

            _failures[key] = new LoginFailure(
                newLockedUntil.HasValue ? 0 : count,
                newLockedUntil);

            if (newLockedUntil.HasValue)
            {
                throw new BusinessException(
                    $"Bạn đã nhập sai {AppConstants.MaxFailedLoginAttempts} lần. " +
                    $"Tài khoản tạm khóa {AppConstants.LoginLockoutMinutes} phút.");
            }

            throw new BusinessException(
                "Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        _failures.TryRemove(key, out _);

        _session.Set(user);

        TryAudit("LOGIN", user.Id, $"Người dùng {user.Username} đăng nhập hệ thống.");

        return user;
    }

    public void Logout()
    {
        var user = _session.User;

        try
        {
            if (user is not null)
                TryAudit("LOGOUT", user.Id, $"Người dùng {user.Username} đăng xuất.");
        }
        finally
        {
            _session.Clear();
        }
    }

    public void ChangePassword(
        int userId,
        string oldPassword,
        string newPassword)
    {
        if (!_session.IsAuthenticated || _session.User?.Id != userId)
        {
            throw new BusinessException(
                "Bạn chỉ có thể đổi mật khẩu của tài khoản đang đăng nhập.");
        }

        if (string.IsNullOrWhiteSpace(oldPassword))
        {
            throw new BusinessException(
                "Vui lòng nhập mật khẩu hiện tại.");
        }

        var user = _users.GetById(userId);

        if (user is null)
        {
            throw new BusinessException(
                "Không tìm thấy tài khoản.");
        }

        if (!VerifyPassword(oldPassword, user.PasswordHash))
        {
            throw new BusinessException(
                "Mật khẩu hiện tại không đúng.");
        }

        _validator.ValidatePassword(newPassword);

        if (VerifyPassword(newPassword, user.PasswordHash))
        {
            throw new BusinessException(
                "Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        var updated = _users.UpdatePassword(
            userId,
            HashPassword(newPassword));

        if (!updated)
        {
            throw new BusinessException(
                "Không thể cập nhật mật khẩu. Vui lòng thử lại.");
        }

        TryAudit(
            "CHANGE_PASSWORD",
            userId,
            $"Người dùng {user.Username} đổi mật khẩu.");
    }

    private void TryAudit(string action, int userId, string detail)
    {
        try
        {
            _audit?.Log(action, "User", userId, detail);
        }
        catch (Exception ex)
        {
            Trace.TraceError(
                "Authentication action {0} for user {1} succeeded, but audit logging failed: {2}",
                action,
                userId,
                ex);
        }
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
        {
            return false;
        }

        try
        {
            return global::BCrypt.Net.BCrypt.Verify(
                password,
                hash);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
