using CinemaManagement.Core.Constants;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL.Database
{
    public class DbSeeder
    {
        private readonly IUserRepository _userRepo;
        private readonly ISettingRepository _settingRepo;

        public DbSeeder(IUserRepository userRepo, ISettingRepository settingRepo)
        {
            _userRepo = userRepo;
            _settingRepo = settingRepo;
        }

        public void Seed()
        {
            if (!_userRepo.IsUsernameExists("admin"))
            {
                _userRepo.Add(new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    FullName = "Quản trị viên Hệ thống",
                    Email = "admin@cinema.vn",
                    Role = UserRole.Admin,
                    IsActive = true
                });
            }

            if (_settingRepo.GetByKey(AppConstants.SettingCinemaName) == null)
            {
                _settingRepo.Upsert(new AppSetting
                {
                    Key = AppConstants.SettingCinemaName,
                    Value = "Galaxy Cinema",
                    Description = "Tên rạp hiển thị trên vé và báo cáo"
                });
            }
        }
    }
}