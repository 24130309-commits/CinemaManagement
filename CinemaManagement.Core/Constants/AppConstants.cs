namespace CinemaManagement.Core.Constants;

public static class AppConstants
{
    public const int DefaultHoldMinutes = 5;
    public const int DefaultCleaningMinutes = 15;
    public const int MaxFailedLoginAttempts = 5;
    public const int LoginLockoutMinutes = 5;
    public const string DatabaseFileName = "cinema_management.db";
    public const string DatabaseDirectoryName = "CinemaManagement";
    public const string PosterDirectoryName = "Resources/Posters";
    public const string DefaultCinemaName = "Cinema Management";
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminPassword = "Admin@123";
    public const string SettingHoldMinutes = "HoldMinutes";
    public const string SettingCleaningMinutes = "CleaningMinutes";
    public const string SettingCinemaName = "CinemaName";
}