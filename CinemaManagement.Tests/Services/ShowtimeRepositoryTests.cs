using Dapper;
using Microsoft.Data.Sqlite;
using CinemaManagement.Core.Entities;
using CinemaManagement.DAL.Database;
using CinemaManagement.DAL.Repositories;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public sealed class ShowtimeRepositoryTests
{
    private SqliteConnection _anchor = null!;
    private ShowtimeRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = $"showtime-tests-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true
        };

        _anchor = new SqliteConnection(builder.ToString());
        _anchor.Open();
        _anchor.Execute(
            """
            CREATE TABLE Movies (Id INTEGER PRIMARY KEY, Title TEXT NOT NULL);
            CREATE TABLE Rooms (
                Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, TotalRows INTEGER NOT NULL,
                TotalCols INTEGER NOT NULL, ScreenType INTEGER NOT NULL, IsActive INTEGER NOT NULL);
            CREATE TABLE Showtimes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, MovieId INTEGER NOT NULL,
                RoomId INTEGER NOT NULL, StartTime TEXT NOT NULL, EndTime TEXT NOT NULL,
                BasePrice NUMERIC NOT NULL, IsActive INTEGER NOT NULL);
            CREATE TABLE Tickets (Id INTEGER PRIMARY KEY, ShowtimeId INTEGER NOT NULL);
            INSERT INTO Movies(Id, Title) VALUES (1, 'Test Movie');
            INSERT INTO Rooms(Id, Name, TotalRows, TotalCols, ScreenType, IsActive)
            VALUES (1, 'Room 1', 5, 8, 0, 1);
            """);

        _repository = new ShowtimeRepository(
            SqliteConnectionFactory.FromConnectionString(builder.ToString()));
    }

    [TearDown]
    public void TearDown()
    {
        _anchor.Dispose();
    }

    [Test]
    public void Insert_RejectsOverlappingShowtimeAtRepositoryBoundary()
    {
        var start = DateTime.Now.AddDays(2);
        _repository.Insert(CreateShowtime(start, start.AddMinutes(120)));

        Assert.Throws<InvalidOperationException>(() =>
            _repository.Insert(CreateShowtime(
                start.AddMinutes(30),
                start.AddMinutes(150))));
    }

    [Test]
    public void Insert_AllowsZeroPriceWhenTheExistingContractAllowsIt()
    {
        var start = DateTime.Now.AddDays(2);
        var showtime = CreateShowtime(start, start.AddMinutes(120));
        showtime.BasePrice = 0;

        Assert.That(_repository.Insert(showtime), Is.GreaterThan(0));
    }

    [Test]
    public void Update_RejectsBasePriceChangeAfterTicketsExist()
    {
        var start = DateTime.Now.AddDays(2);
        var id = _repository.Insert(CreateShowtime(start, start.AddMinutes(120)));
        _anchor.Execute(
            "INSERT INTO Tickets(Id, ShowtimeId) VALUES (1, @Id);",
            new { Id = id });

        var showtime = _repository.GetById(id)!;
        showtime.BasePrice += 1000;

        Assert.Throws<InvalidOperationException>(() =>
            _repository.Update(showtime));
    }

    [Test]
    public void Update_AllowsDeactivationAfterItsRoomIsDeactivated()
    {
        var start = DateTime.Now.AddDays(2);
        var id = _repository.Insert(CreateShowtime(start, start.AddMinutes(120)));
        _anchor.Execute("UPDATE Rooms SET IsActive = 0 WHERE Id = 1;");

        var showtime = _repository.GetById(id)!;
        showtime.IsActive = false;

        Assert.That(_repository.Update(showtime), Is.True);
    }

    private static Showtime CreateShowtime(DateTime start, DateTime end)
    {
        return new Showtime
        {
            MovieId = 1,
            RoomId = 1,
            StartTime = start,
            EndTime = end,
            BasePrice = 80000,
            IsActive = true
        };
    }
}
