namespace CinemaManagement.Core.Interfaces;

public interface ISettingService
{
    int GetHoldMinutes();
    int GetCleaningMinutes();
    string GetCinemaName();
    void Set(string key, string value);
}