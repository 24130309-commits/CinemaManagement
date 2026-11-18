
using System;
using System.Collections.Generic;
using System.Linq;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public sealed class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repository;
    private readonly CurrentUserSession _session;

    public AuditService(
        IAuditLogRepository repository,
        CurrentUserSession session)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));

        _session = session
            ?? throw new ArgumentNullException(nameof(session));
    }

    // API hiện có của A
    public void Log(string action, string detail)
    {
        _repository.Add(new AuditLog
        {
            UserId = _session.User?.Id ?? 0,
            Action = action,
            Detail = detail,
            CreatedAt = DateTime.Now
        });
    }

    public IEnumerable<AuditLog> Search(
        int userId,
        DateTime from,
        DateTime to)
    {
        return _repository.Search(userId, from, to);
    }

    // API bổ sung từ B
    public void Log(
        string action,
        string entityName,
        int? entityId,
        string details)
    {
        string detail = $"Entity: {entityName ?? "Unknown"}";

        if (entityId.HasValue)
            detail += $" | EntityId: {entityId.Value}";

        if (!string.IsNullOrWhiteSpace(details))
            detail += $" | Details: {details}";

        _repository.Add(new AuditLog
        {
            UserId = _session.User?.Id ?? 0,
            Action = action,
            Detail = detail,
            CreatedAt = DateTime.Now
        });
    }

    public IEnumerable<AuditLog> GetRecentLogs(int limit = 100)
    {
        if (limit <= 0)
            return Enumerable.Empty<AuditLog>();

        return _repository.GetAll()
            .OrderByDescending(log => log.CreatedAt)
            .Take(limit)
            .ToList();
    }
}
