using System;
using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IAuditService
{
    void Log(
        string action,
        string entityName,
        int? entityId,
        string details);

    void Log(string action)
    {
        Log(action, string.Empty, null, string.Empty);
    }

    void Log(string action, string detail)
    {
        Log(action, string.Empty, null, detail);
    }

    IEnumerable<AuditLog> Search(
        int userId,
        DateTime from,
        DateTime to);

    IEnumerable<AuditLog> GetRecentLogs(int limit = 100);
}