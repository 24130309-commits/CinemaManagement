using CinemaManagement.Core.Constants;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public sealed class SettingService : ISettingService
{
    private readonly ISettingRepository _repository;

    public SettingService(ISettingRepository repository)
    {
        _repository = repository;
    }

    public int GetHoldMinutes() =>
        ReadPositiveInt(
            AppConstants.SettingHoldMinutes,
            AppConstants.DefaultHoldMinutes,
            1,
            30);

    public int GetCleaningMinutes() =>
        ReadPositiveInt(
            AppConstants.SettingCleaningMinutes,
            AppConstants.DefaultCleaningMinutes,
            0,
            180);

    public string GetCinemaName() =>
        _repository.Get(AppConstants.SettingCinemaName)
            is { Length: > 0 } value
                ? value
                : AppConstants.DefaultCinemaName;

    public void Set(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new BusinessException("Tên cấu hình không hợp lệ.");

        if (key == AppConstants.SettingHoldMinutes &&
            (!int.TryParse(value, out var hold) ||
             hold is < 1 or > 30))
        {
            throw new BusinessException(
                "Thời gian giữ ghế phải từ 1 đến 30 phút.");
        }

        if (key == AppConstants.SettingCleaningMinutes &&
            (!int.TryParse(value, out var clean) ||
             clean is < 0 or > 180))
        {
            throw new BusinessException(
                "Thời gian dọn phòng phải từ 0 đến 180 phút.");
        }

        if (key == AppConstants.SettingCinemaName &&
            string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessException("Tên rạp không được để trống.");
        }

        _repository.Set(key, value.Trim());
    }

    private int ReadPositiveInt(
        string key,
        int fallback,
        int min,
        int max)
    {
        return int.TryParse(_repository.Get(key), out var value) &&
               value >= min &&
               value <= max
            ? value
            : fallback;
    }
}