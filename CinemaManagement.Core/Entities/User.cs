using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Entities
{
    public class User
    {
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Fulname { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    }
}
    
