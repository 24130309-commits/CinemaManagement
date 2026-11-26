
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
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

    public void Log(string action, string detail)
    {
        Log(action, string.Empty, null, detail);
    }

    public void Log(
        string action,
        string entityName,
        int? entityId,
        string details)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new BusinessException("Tên hành động nhật ký không hợp lệ.");

        if (entityId.HasValue && entityId.Value <= 0)
            throw new BusinessException("Mã đối tượng nhật ký không hợp lệ.");

        _repository.Add(new AuditLog
        {
            Username = _session.User?.Username ?? "System",
            UserId = _session.User?.Id,
            Action = action.Trim(),
            EntityName = entityName?.Trim() ?? string.Empty,
            EntityId = entityId,
            Detail = details?.Trim() ?? string.Empty,
            CreatedAt = DateTime.Now
        });
    }

    public IEnumerable<AuditLog> Search(
        int userId,
        DateTime from,
        DateTime to)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        if (userId < 0)
            throw new BusinessException("Mã người dùng không hợp lệ.");

        if (from.Date > to.Date)
        {
            throw new BusinessException(
                "Ngày bắt đầu không được sau ngày kết thúc.");
        }

        return _repository.Search(userId, from, to);
    }

    public IEnumerable<AuditLog> GetRecentLogs(int limit = 100)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        if (limit is < 1 or > 1000)
        {
            throw new BusinessException(
                "Số lượng nhật ký cần lấy phải từ 1 đến 1000.");
        }

        return _repository.GetAll()
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Take(limit)
            .ToList();
    }
}
