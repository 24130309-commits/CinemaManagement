using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IAuditService
{
    // API hiện có của A
    void Log(string action, string detail);
    IEnumerable<AuditLog> Search(
        int userId,
        DateTime from,
        DateTime to);

    // API bổ sung từ B
    void Log(
        string action,
        string entityName,
        int? entityId,
        string details);

    IEnumerable<AuditLog> GetRecentLogs(int limit = 100);
}