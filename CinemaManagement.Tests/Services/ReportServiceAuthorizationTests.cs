using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public sealed class ReportServiceAuthorizationTests
{
    [Test]
    public void GetRevenueByMovie_RejectsAnonymousSession()
    {
        var reports = new FakeReportRepository();
        var service = new ReportService(
            reports,
            SqliteConnectionFactory.FromConnectionString("Data Source=:memory:"),
            new CurrentUserSession());

        Assert.Throws<BusinessException>(() =>
            service.GetRevenueByMovie(DateTime.Today, DateTime.Today));
        Assert.That(reports.QueryCount, Is.Zero);
    }

    [Test]
    public void GetRevenueByMovie_AllowsAuthenticatedStaff()
    {
        var reports = new FakeReportRepository();
        var session = new CurrentUserSession();
        session.Set(new User { Id = 1, Username = "staff", Role = UserRole.Staff });
        var service = new ReportService(
            reports,
            SqliteConnectionFactory.FromConnectionString("Data Source=:memory:"),
            session);

        Assert.That(service.GetRevenueByMovie(DateTime.Today, DateTime.Today), Is.Empty);
        Assert.That(reports.QueryCount, Is.EqualTo(1));
    }

    private sealed class FakeReportRepository : IReportRepository
    {
        public int QueryCount { get; private set; }

        public IEnumerable<RevenueByMovieDto> RevenueByMovie(DateTime from, DateTime to)
        {
            QueryCount++;
            return [];
        }

        public IEnumerable<OccupancyDto> OccupancyByShowtime(DateTime from, DateTime to) => [];
        public IEnumerable<RevenueByDayDto> RevenueByDay(DateTime from, DateTime to) => [];
        public IEnumerable<RevenueByMovieDto> TopMovies(int n) => [];
    }
}
