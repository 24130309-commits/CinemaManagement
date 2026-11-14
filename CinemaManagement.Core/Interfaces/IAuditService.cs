using System;
using System.Collections.Generic;
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces
{
    public partial interface IAuthService
    {
        User Login(string username, string password);
        void Logout();
        void ChangePassword(int userId, string oldPassword, string newPassword);
    }

    public partial interface  IUserService
    {
        IEnumerable<User> GetAllUsers();
        User GetUserById(int id);
        void CreateUser(User user, string plainPassword);
        void UpdateUser(User user);
        void ResetPassword(int userId, string newPassword);
        void ToggleUserStatus(int userId);
    }

    public interface IAuditService
    {
        void Log(string action, string entityName = null, int? entityId = null, string details = null);
        IEnumerable<AuditLog> GetRecentLogs(int limit = 100);
    }

    public partial interface ISettingService
    {
        IEnumerable<AppSetting> GetAllSettings();
        string GetValue(string key, string defaultValue = "");
        void SaveSetting(string key, string value, string description = null);
    }

    public partial interface IBackupService
    {
        void BackupDatabase(string destinationFilePath);
        void RestoreDatabase(string backupFilePath);
    }

    public partial interface IReportService
    {
        IEnumerable<RevenueByMovieDto> GetRevenueByMovie(DateTime fromDate, DateTime toDate);
        IEnumerable<OccupancyDto> GetOccupancyReport(DateTime fromDate, DateTime toDate);
    }

    public partial interface IExportService
    {
        void ExportToCsv<T>(IEnumerable<T> data, string filePath);
    }
}