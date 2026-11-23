using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;

namespace CinemaManagement.BLL.Session;

public sealed class CurrentUserSession
{
    public User? User { get; private set; }

    public User? CurrentUser => User;

    public UserRole? Role => User?.Role;

    public bool IsAuthenticated => User is not null;

    public bool IsLoggedIn => IsAuthenticated;

    public bool IsAdmin => User?.Role == UserRole.Admin;

    public void Set(User user)
    {
        User = user ?? throw new ArgumentNullException(nameof(user));
    }

    public void SetUser(User user)
    {
        Set(user);
    }

    public bool IsInRole(params UserRole[] roles)
    {
        return User is not null && roles.Contains(User.Role);
    }

    public void Clear()
    {
        User = null;
    }
}