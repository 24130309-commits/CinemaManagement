using System;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;

namespace CinemaManagement.BLL
{
    public static class CurrentUserSession
    {
        public static User CurrentUser { get; private set; }
        public static bool IsLoggedIn => CurrentUser != null;

        public static bool IsAdmin
        {
            get
            {
                if (CurrentUser == null) return false;

                // Tự động xử lý dù thuộc tính Role trong User là enum UserRole hay kiểu string
                var roleProp = CurrentUser.GetType().GetProperty("Role") 
                               ?? CurrentUser.GetType().GetProperty("UserRole");

                if (roleProp == null) return false;

                var roleValue = roleProp.GetValue(CurrentUser)?.ToString();
                return string.Equals(roleValue, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        public static void SetUser(User user) => CurrentUser = user;
        public static void Clear() => CurrentUser = null;
    }
}