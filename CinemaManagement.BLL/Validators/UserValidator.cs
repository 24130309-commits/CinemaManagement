using System.Text.RegularExpressions;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;

namespace CinemaManagement.BLL.Validators;

public sealed class UserValidator
{
    public void Validate(
        User user,
        bool validatePassword = false,
        string? password = null)
    {
        if (user is null)
            throw new BusinessException("Thông tin người dùng không hợp lệ.");

        if (string.IsNullOrWhiteSpace(user.Username) ||
            user.Username.Trim().Length < 4 ||
            user.Username.Length > 30 ||
            !Regex.IsMatch(user.Username, "^[a-zA-Z0-9._-]+$"))
        {
            throw new BusinessException(
                "Tên đăng nhập phải từ 4–30 ký tự, chỉ gồm chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.");
        }

        if (!Enum.IsDefined(user.Role))
            throw new BusinessException("Vai trò người dùng không hợp lệ.");

        if (string.IsNullOrWhiteSpace(user.FullName) ||
            user.FullName.Trim().Length < 2 ||
            user.FullName.Length > 100)
        {
            throw new BusinessException("Họ tên phải có từ 2 đến 100 ký tự.");
        }

        if (validatePassword)
            ValidatePassword(password ?? string.Empty);
    }

    public void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < 8 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
        {
            throw new BusinessException(
                "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường và chữ số.");
        }
    }
}