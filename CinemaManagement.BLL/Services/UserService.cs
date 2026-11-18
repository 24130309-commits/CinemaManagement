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

    public IEnumerable<User> GetAll() => _users.GetAll();

    public void Create(User user)
    {
        _validator.Validate(user);

        if (_users.GetByUsername(user.Username) is not null)
            throw new BusinessException("Tên đăng nhập đã tồn tại.");

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            throw new BusinessException("Mật khẩu ban đầu không được để trống.");

        user.PasswordHash = _auth.HashPassword(user.PasswordHash);
        user.Username = user.Username.Trim();
        user.FullName = user.FullName.Trim();
        user.IsActive = true;

        _users.Insert(user);
    }

    public void Update(User user)
    {
        _validator.Validate(user);

        var duplicate = _users.GetByUsername(user.Username);

        if (duplicate is not null && duplicate.Id != user.Id)
            throw new BusinessException("Tên đăng nhập đã được sử dụng.");

        user.Username = user.Username.Trim();
        user.FullName = user.FullName.Trim();

        if (!_users.Update(user))
            throw new BusinessException(
                "Không tìm thấy người dùng cần cập nhật.");
    }

    public void ResetPassword(int userId, string newPassword)
    {
        _validator.ValidatePassword(newPassword);

        if (!_users.UpdatePassword(
                userId,
                _auth.HashPassword(newPassword)))
        {
            throw new BusinessException("Không tìm thấy tài khoản.");
        }
    }

    public void SetActive(int userId, bool isActive)
    {
        if (_session.User?.Id == userId && !isActive)
            throw new BusinessException(
                "Bạn không thể tự khóa tài khoản đang đăng nhập.");

        var user = _users.GetById(userId)
            ?? throw new BusinessException("Không tìm thấy tài khoản.");

        if (user.Role == UserRole.Admin &&
            isActive == false &&
            _users.GetAll().Count(x =>
                x.Role == UserRole.Admin && x.IsActive) <= 1)
        {
            throw new BusinessException(
                "Không thể khóa tài khoản Admin đang hoạt động cuối cùng.");
        }

        if (!_users.SetActive(userId, isActive))
            throw new BusinessException(
                "Không thể cập nhật trạng thái tài khoản.");
    }
}