
using CinemaManagement.Core.Constants;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.BLL.Services;

public sealed class SettingService : ISettingService
{
    private readonly ISettingRepository _repository;
    private readonly CurrentUserSession _session;

    public SettingService(
        ISettingRepository repository,
        CurrentUserSession session)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        _session = session
            ?? throw new ArgumentNullException(nameof(session));
    }

    public int GetHoldMinutes()
    {
        return ReadInt(
            AppConstants.SettingHoldMinutes,
            AppConstants.DefaultHoldMinutes,
            1,
            30);
    }

    public int GetCleaningMinutes()
    {
        return ReadInt(
            AppConstants.SettingCleaningMinutes,
            AppConstants.DefaultCleaningMinutes,
            0,
            180);
    }

    public string GetCinemaName()
    {
        var value = _repository.Get(AppConstants.SettingCinemaName);

        return string.IsNullOrWhiteSpace(value)
            ? AppConstants.DefaultCinemaName
            : value.Trim();
    }

    public string? GetValue(string key)
    {
        ValidateKey(key);
        return _repository.Get(key.Trim());
    }

    public string GetValue(string key, string defaultValue)
    {
        if (defaultValue is null)
            throw new BusinessException("Giá trị mặc định không hợp lệ.");

        var value = GetValue(key);

        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value;
    }

    public void SaveSetting(string key, string value)
    {
        Set(key, value);
    }

    public void SaveOperationalSettings(
        string cinemaName,
        int holdMinutes,
        int cleaningMinutes)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "thay đổi cấu hình");

        var name = cinemaName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name) || name.Length > 500)
            throw new BusinessException("Tên rạp không được để trống và không quá 500 ký tự.");
        if (holdMinutes is < 1 or > 30)
            throw new BusinessException("Thời gian giữ ghế phải từ 1 đến 30 phút.");
        if (cleaningMinutes is < 0 or > 180)
            throw new BusinessException("Thời gian dọn phòng phải từ 0 đến 180 phút.");

        _repository.SetMany(
        [
            new AppSetting { Key = AppConstants.SettingCinemaName, Value = name },
            new AppSetting { Key = AppConstants.SettingHoldMinutes, Value = holdMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            new AppSetting { Key = AppConstants.SettingCleaningMinutes, Value = cleaningMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture) }
        ]);
    }

    public void Set(string key, string value)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "thay đổi cấu hình");
        ValidateKey(key);

        if (value is null)
            throw new BusinessException("Giá trị cấu hình không hợp lệ.");

        key = key.Trim();
        value = value.Trim();

        if (key == AppConstants.SettingHoldMinutes)
        {
            ValidateIntegerSetting(
                value,
                "Thời gian giữ ghế phải từ 1 đến 30 phút.",
                1,
                30);
        }

        if (key == AppConstants.SettingCleaningMinutes)
        {
            ValidateIntegerSetting(
                value,
                "Thời gian dọn phòng phải từ 0 đến 180 phút.",
                0,
                180);
        }

        if (key == AppConstants.SettingCinemaName &&
            string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessException(
                "Tên rạp không được để trống.");
        }

        if (value.Length > 500)
        {
            throw new BusinessException(
                "Giá trị cấu hình không được vượt quá 500 ký tự.");
        }

        _repository.Set(key, value);
    }

    private int ReadInt(
        string key,
        int fallback,
        int min,
        int max)
    {
        var rawValue = _repository.Get(key);

        return int.TryParse(rawValue, out var value) &&
               value >= min &&
               value <= max
            ? value
            : fallback;
    }

    private static void ValidateIntegerSetting(
        string value,
        string message,
        int min,
        int max)
    {
        if (!int.TryParse(value, out var number) ||
            number < min ||
            number > max)
        {
            throw new BusinessException(message);
        }
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) ||
            key.Trim().Length > 100)
        {
            throw new BusinessException(
                "Tên cấu hình không hợp lệ.");
        }
    }
}
