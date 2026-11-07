using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface ISettingRepository
{
    string Get(string key);
    void Set(string key, string value);
    IEnumerable<AppSetting> GetAll();
}