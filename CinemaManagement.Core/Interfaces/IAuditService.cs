using System;
using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IAuditService
{
    void Log(string action, string detail);
    IEnumerable<AuditLog> Search(int userId, DateTime from, DateTime to);
}