using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;

namespace CinemaManagement.BLL.Session;

public sealed class CurrentUserSession
{
    public User? User { get; private set; }
    public UserRole? Role => User?.Role;
    public bool IsAuthenticated => User is not null;

    public void Set(User user) =>
        User = user ?? throw new ArgumentNullException(nameof(user));

    public bool IsInRole(params UserRole[] roles) =>
        User is not null && roles.Contains(User.Role);

    public void Clear() => User = null;
}