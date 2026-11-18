namespace CinemaManagement.Core.Constants;

public static class AppConstants
{
    // Cấu hình database từ A
    public const string DatabaseFileName = "cinema_management.db";
    public const string DatabaseDirectoryName = "CinemaManagement";

    // Chuỗi kết nối từ B
    public const string ConnectionString = "Data Source=CinemaManagement.db;";

    // Thư mục và thông tin mặc định từ A
    public const string PosterDirectoryName = "Resources/Posters";
    public const string DefaultCinemaName = "Cinema Management";
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminPassword = "Admin@123";

    // Cấu hình giữ ghế và khóa đăng nhập từ A
    public const int DefaultHoldMinutes = 5;
    public const int DefaultCleaningMinutes = 15;
    public const int MaxFailedLoginAttempts = 5;
    public const int LoginLockoutMinutes = 5;

    // Các khóa cài đặt từ A và B
    public const string SettingHoldMinutes = "HoldMinutes";
    public const string SettingCleaningMinutes = "CleaningMinutes";
    public const string SettingCinemaName = "CinemaName";
    public const string SettingDefaultTicketPrice = "DefaultTicketPrice";
    public const string SettingThemeMode = "ThemeMode";
}