// CinemaManagement.Core/Interfaces/IRepository.cs
using System.Collections.Generic;

namespace CinemaManagement.Core.Interfaces
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> GetAll();
        T GetById(int id);
        void Add(T entity);
        void Update(T entity);
        void Delete(int id);
    }

    public partial interface IUserRepository : IRepository<Entities.User>
    {
        Entities.User GetByUsername(string username);
        bool IsUsernameExists(string username, int? excludeId = null);
    }

    public partial interface IAuditLogRepository : IRepository<Entities.AuditLog>
    {
        IEnumerable<Entities.AuditLog> GetRecentLogs(int limit = 100);
    }

    public partial interface ISettingRepository
    {
        IEnumerable<Entities.AppSetting> GetAll();
        Entities.AppSetting GetByKey(string key);
        void Upsert(Entities.AppSetting setting);
    }

    public partial interface IReportRepository
    {
        IEnumerable<DTOs.RevenueByMovieDto> GetRevenueByMovie(System.DateTime fromDate, System.DateTime toDate);
        IEnumerable<DTOs.OccupancyDto> GetOccupancyReport(System.DateTime fromDate, System.DateTime toDate);
    }

    public interface IUnitOfWork : System.IDisposable
    {
        void BeginTransaction();
        void Commit();
        void Rollback();
    }
}