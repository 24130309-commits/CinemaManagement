using System;
using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    void Add(AuditLog log);
    IEnumerable<AuditLog> Search(int userId, DateTime from, DateTime to);
}