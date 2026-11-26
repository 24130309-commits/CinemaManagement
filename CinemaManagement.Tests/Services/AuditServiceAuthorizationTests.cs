using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public sealed class AuditServiceAuthorizationTests
{
    [Test]
    public void Search_RejectsAnonymousSessionBeforeReadingLogs()
    {
        var repository = new FakeAuditLogRepository();
        var service = new AuditService(repository, new CurrentUserSession());

        Assert.Throws<BusinessException>(() =>
            service.Search(0, DateTime.Today, DateTime.Today));
        Assert.That(repository.ReadCount, Is.Zero);
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public int ReadCount { get; private set; }
        public AuditLog? GetById(int id) => null;
        public IEnumerable<AuditLog> GetAll() { ReadCount++; return []; }
        public int Insert(AuditLog entity) => 1;
        public bool Update(AuditLog entity) => true;
        public bool Delete(int id) => true;
        public void Add(AuditLog log) { }
        public IEnumerable<AuditLog> Search(int userId, DateTime from, DateTime to)
        {
            ReadCount++;
            return [];
        }
    }
}
