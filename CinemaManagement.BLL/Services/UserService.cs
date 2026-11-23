using CinemaManagement.BLL.Session;
using CinemaManagement.BLL.Validators;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IAuthService _auth;
    private readonly UserValidator _validator;
    private readonly CurrentUserSession _session;

    public UserService(
        IUserRepository users,
        IAuthService auth,
        UserValidator validator,
        CurrentUserSession session)
    {
        _users = users;
        _auth = auth;
        _validator = validator;
        _session = session;
    }

    public IEnumerable<User> GetAllUsers()
    {
        return _users.GetAll();
    }

    public User GetUserById(int id)
    {
        return _users.GetById(id)
            ?? throw new BusinessException(
                "Không tìm thấy tài khoản.");
    }

    public void CreateUser(User user, string plainPassword)
    {
        _validator.Validate(user);

        if (_users.GetByUsername(user.Username) is not null)
        {
            throw new BusinessException(
                "Tên đăng nhập đã tồn tại.");
        }

        if (string.IsNullOrWhiteSpace(plainPassword))
        {
            throw new BusinessException(
                "Mật khẩu ban đầu không được để trống.");
        }

        user.Username = user.Username.Trim();
        user.FullName = user.FullName.Trim();
        user.PasswordHash = _auth.HashPassword(plainPassword);
        user.IsActive = true;

        _users.Insert(user);
    }

    public void UpdateUser(User user)
    {
        _validator.Validate(user);

        var duplicate = _users.GetByUsername(user.Username);

        if (duplicate is not null && duplicate.Id != user.Id)
        {
            throw new BusinessException(
                "Tên đăng nhập đã được sử dụng.");
        }

        user.Username = user.Username.Trim();
        user.FullName = user.FullName.Trim();

        if (!_users.Update(user))
        {
            throw new BusinessException(
                "Không tìm thấy người dùng cần cập nhật.");
        }
    }

    public void ResetPassword(
        int userId,
        string newPassword)
    {
        _validator.ValidatePassword(newPassword);

        if (!_users.UpdatePassword(
                userId,
                _auth.HashPassword(newPassword)))
        {
            throw new BusinessException(
                "Không tìm thấy tài khoản.");
        }
    }

    public void ToggleUserStatus(int userId)
    {
        if (_session.User?.Id == userId)
        {
            throw new BusinessException(
                "Bạn không thể thay đổi trạng thái tài khoản đang đăng nhập.");
        }

        var user = _users.GetById(userId)
            ?? throw new BusinessException(
                "Không tìm thấy tài khoản.");

        bool newStatus = !user.IsActive;

        if (user.Role == UserRole.Admin &&
            !newStatus &&
            _users.GetAll().Count(x =>
                x.Role == UserRole.Admin &&
                x.IsActive) <= 1)
        {
            throw new BusinessException(
                "Không thể khóa tài khoản Admin đang hoạt động cuối cùng.");
        }

        if (!_users.SetActive(userId, newStatus))
        {
            throw new BusinessException(
                "Không thể cập nhật trạng thái tài khoản.");
        }
    }
}