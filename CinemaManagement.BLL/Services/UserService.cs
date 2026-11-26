
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
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public IEnumerable<User> GetAllUsers()
    {
        EnsureAdministrator();
        return _users.GetAll();
    }

    public User GetUserById(int id)
    {
        EnsureAdministrator();

        if (id <= 0)
            throw new BusinessException("Mã tài khoản không hợp lệ.");

        return _users.GetById(id)
            ?? throw new BusinessException("Không tìm thấy tài khoản.");
    }

    public void CreateUser(User user, string plainPassword)
    {
        EnsureAdministrator();

        if (user is null)
            throw new BusinessException("Thông tin tài khoản không hợp lệ.");

        user.Username = user.Username?.Trim() ?? string.Empty;
        user.FullName = user.FullName?.Trim() ?? string.Empty;
        user.Email = user.Email?.Trim() ?? string.Empty;

        _validator.Validate(user);
        _validator.ValidatePassword(plainPassword);

        if (_users.GetByUsername(user.Username) is not null)
        {
            throw new BusinessException(
                "Tên đăng nhập đã tồn tại.");
        }

        user.PasswordHash = _auth.HashPassword(plainPassword);
        user.IsActive = true;
        user.CreatedAt = DateTime.Now;

        var id = _users.Insert(user);

        if (id <= 0)
            throw new BusinessException("Không thể tạo tài khoản.");

        user.Id = id;
    }

    public void UpdateUser(User user)
    {
        EnsureAdministrator();

        if (user is null)
            throw new BusinessException("Thông tin tài khoản không hợp lệ.");

        if (user.Id <= 0)
            throw new BusinessException("Mã tài khoản không hợp lệ.");

        user.Username = user.Username?.Trim() ?? string.Empty;
        user.FullName = user.FullName?.Trim() ?? string.Empty;
        user.Email = user.Email?.Trim() ?? string.Empty;

        _validator.Validate(user);

        var existing = _users.GetById(user.Id)
            ?? throw new BusinessException("Không tìm thấy tài khoản.");

        var duplicate = _users.GetByUsername(user.Username);

        if (duplicate is not null && duplicate.Id != user.Id)
        {
            throw new BusinessException(
                "Tên đăng nhập đã được sử dụng.");
        }

        // Không cho phép tự khóa hoặc tự hạ quyền tài khoản đang đăng nhập.
        if (_session.User?.Id == user.Id)
        {
            if (!user.IsActive || user.Role != existing.Role)
            {
                throw new BusinessException(
                    "Bạn không thể tự khóa hoặc thay đổi vai trò tài khoản đang đăng nhập.");
            }
        }

        var isRemovingLastAdmin =
            existing.Role == UserRole.Admin &&
            existing.IsActive &&
            (!user.IsActive || user.Role != UserRole.Admin);

        if (isRemovingLastAdmin)
        {
            var activeAdminCount = _users.GetAll()
                .Count(x => x.Role == UserRole.Admin && x.IsActive);

            if (activeAdminCount <= 1)
            {
                throw new BusinessException(
                    "Không thể hạ quyền hoặc khóa tài khoản Admin đang hoạt động cuối cùng.");
            }
        }

        // Không thay đổi mật khẩu tại chức năng cập nhật thông tin.
        user.PasswordHash = existing.PasswordHash;
        user.CreatedAt = existing.CreatedAt;

        if (!_users.Update(user))
        {
            throw new BusinessException(
                "Không thể cập nhật tài khoản.");
        }
    }

    public void ResetPassword(int userId, string newPassword)
    {
        EnsureAdministrator();

        if (userId <= 0)
            throw new BusinessException("Mã tài khoản không hợp lệ.");

        _ = _users.GetById(userId)
            ?? throw new BusinessException("Không tìm thấy tài khoản.");

        _validator.ValidatePassword(newPassword);

        if (!_users.UpdatePassword(
                userId,
                _auth.HashPassword(newPassword)))
        {
            throw new BusinessException(
                "Không thể cập nhật mật khẩu.");
        }
    }

    public void ToggleUserStatus(int userId)
    {
        EnsureAdministrator();

        if (userId <= 0)
            throw new BusinessException("Mã tài khoản không hợp lệ.");

        if (_session.User?.Id == userId)
        {
            throw new BusinessException(
                "Bạn không thể thay đổi trạng thái tài khoản đang đăng nhập.");
        }

        var user = _users.GetById(userId)
            ?? throw new BusinessException("Không tìm thấy tài khoản.");

        var newStatus = !user.IsActive;

        if (user.Role == UserRole.Admin && !newStatus)
        {
            var activeAdminCount = _users.GetAll()
                .Count(x => x.Role == UserRole.Admin && x.IsActive);

            if (activeAdminCount <= 1)
            {
                throw new BusinessException(
                    "Không thể khóa tài khoản Admin đang hoạt động cuối cùng.");
            }
        }

        if (!_users.SetActive(userId, newStatus))
        {
            throw new BusinessException(
                "Không thể cập nhật trạng thái tài khoản.");
        }
    }

    private void EnsureAdministrator()
    {
        if (!_session.IsAdmin)
        {
            throw new BusinessException(
                "Bạn không có quyền thực hiện thao tác quản trị người dùng.");
        }
    }
}
