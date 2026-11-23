namespace CinemaManagement.Core.Interfaces;

public partial interface ISettingService
{
    int GetHoldMinutes();
    int GetCleaningMinutes();
    string GetCinemaName();

    string? GetValue(string key);
    string GetValue(string key, string defaultValue);

    void Set(string key, string value);
    void SaveSetting(string key, string value);
}