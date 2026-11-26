using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;

namespace CinemaManagement.BLL.Services;

internal static class AuthorizationGuard
{
    public static void EnsureAuthenticated(CurrentUserSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!session.IsAuthenticated)
        {
            throw new BusinessException(
                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }
    }

    public static void EnsureAdministrator(
        CurrentUserSession session,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!session.IsInRole(UserRole.Admin))
        {
            throw new BusinessException(
                $"Bạn không có quyền {operation}.");
        }
    }
}
