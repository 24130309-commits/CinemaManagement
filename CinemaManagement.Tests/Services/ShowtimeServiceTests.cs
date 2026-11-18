using CinemaManagement.BLL.Services;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public class ShowtimeServiceTests
{
    [Test]
    public void AddShowtime_ComputesEndTimeIncludingCleaningBuffer()
    {
        var repo = new FakeShowtimeRepository();
        var service = CreateService(repo);

        var start = DateTime.Now.AddDays(2);

        var item = new Showtime
        {
            MovieId = 1,
            RoomId = 1,
            StartTime = start,
            BasePrice = 80000
        };

        service.AddShowtime(item);

        Assert.That(
            item.EndTime,
            Is.EqualTo(start.AddMinutes(135))
                .Within(TimeSpan.FromSeconds(1)));

        Assert.That(repo.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddShowtime_RejectsExactOverlap()
    {
        var repo = new FakeShowtimeRepository();
        var service = CreateService(repo);

        var start = DateTime.Now.AddDays(2);

        repo.Items.Add(new Showtime
        {
            Id = 9,
            MovieId = 1,
            RoomId = 1,
            StartTime = start,
            EndTime = start.AddMinutes(140),
            IsActive = true
        });

        var candidate = new Showtime
        {
            MovieId = 1,
            RoomId = 1,
            StartTime = start,
            BasePrice = 50000
        };

        Assert.Throws<ShowtimeConflictException>(
            () => service.AddShowtime(candidate));
    }

    [Test]
    public void AddShowtime_RejectsContainedOverlap()
    {
        var repo = new FakeShowtimeRepository();
        var service = CreateService(repo);

        var start = DateTime.Now.AddDays(2);

        repo.Items.Add(new Showtime
        {
            Id = 9,
            MovieId = 1,
            RoomId = 1,
            StartTime = start,
            EndTime = start.AddMinutes(150),
            IsActive = true
        });

        Assert.Throws<ShowtimeConflictException>(
            () => service.AddShowtime(new Showtime
            {
                MovieId = 1,
                RoomId = 1,
                StartTime = start.AddMinutes(20),
                BasePrice = 50000
            }));
    }

    [Test]
    public void AddShowtime_AllowsAdjacentShowtimes()
    {
        var repo = new FakeShowtimeRepository();
        var service = CreateService(repo);

        var firstStart = DateTime.Now.AddDays(2);
        var firstEnd = firstStart.AddMinutes(135);

        repo.Items.Add(new Showtime
        {
            Id = 9,
            MovieId = 1,
            RoomId = 1,
            StartTime = firstStart.AddMinutes(-135),
            EndTime = firstStart,
            IsActive = true
        });

        var candidate = new Showtime
        {
            MovieId = 1,
            RoomId = 1,
            StartTime = firstStart,
            BasePrice = 50000
        };

        service.AddShowtime(candidate);

        Assert.That(
            candidate.EndTime,
            Is.EqualTo(firstEnd)
                .Within(TimeSpan.FromSeconds(1)));
    }

    private static ShowtimeService CreateService(
        FakeShowtimeRepository repo)
    {
        return new ShowtimeService(
            repo,
            new FakeMovieRepository(),
            new FakeRoomRepository());
    }

    private sealed class FakeShowtimeRepository
        : IShowtimeRepository
    {
        public List<Showtime> Items { get; } = new();

        public Showtime? GetById(int id)
        {
            return Items.FirstOrDefault(x => x.Id == id);
        }

        public IEnumerable<Showtime> GetAll()
        {
            return Items;
        }

        public int Insert(Showtime entity)
        {
            entity.Id = Items.Count + 1;
            Items.Add(entity);

            return entity.Id;
        }

        public bool Update(Showtime entity)
        {
            var old = GetById(entity.Id);

            if (old == null)
                return false;

            Items.Remove(old);
            Items.Add(entity);

            return true;
        }

        public bool Delete(int id)
        {
            var item = GetById(id);

            if (item == null)
                return false;

            Items.Remove(item);

            return true;
        }

        public IEnumerable<Showtime> GetByMovieId(int movieId)
        {
            return Items.Where(x => x.MovieId == movieId);
        }

        public IEnumerable<Showtime> GetByDate(DateTime date)
        {
            return Items.Where(
                x => x.StartTime.Date == date.Date);
        }

        public bool HasConflict(
            int roomId,
            DateTime startTime,
            DateTime endTime,
            int? excludeShowtimeId = null)
        {
            return Items.Any(x =>
                x.IsActive &&
                x.RoomId == roomId &&
                x.Id != excludeShowtimeId &&
                x.StartTime < endTime &&
                x.EndTime > startTime);
        }

        public bool HasTickets(int showtimeId)
        {
            return false;
        }
    }

    private sealed class FakeMovieRepository
        : IMovieRepository
    {
        private readonly Movie _movie = new()
        {
            Id = 1,
            Title = "Test",
            DurationMinutes = 120,
            ReleaseDate = DateTime.Today
        };

        public Movie? GetById(int id)
        {
            return id == 1 ? _movie : null;
        }

        public IEnumerable<Movie> GetAll()
        {
            return new[] { _movie };
        }

        public int Insert(Movie entity)
        {
            return 1;
        }

        public bool Update(Movie entity)
        {
            return true;
        }

        public bool Delete(int id)
        {
            return true;
        }

        public IEnumerable<Movie> GetMoviesByStatus(
            MovieStatus status)
        {
            return new[] { _movie }
                .Where(x => x.Status == status);
        }

        public IEnumerable<Movie> Search(
            string keyword,
            int? genreId = null,
            MovieStatus? status = null)
        {
            return new[] { _movie };
        }

        public bool IsTitleExists(
            string title,
            int? excludeId = null)
        {
            return false;
        }

        public IEnumerable<int> GetGenreIds(int movieId)
        {
            return Array.Empty<int>();
        }

        public void SetGenres(
            int movieId,
            IEnumerable<int> genreIds)
        {
        }
    }

    private sealed class FakeRoomRepository
        : IRoomRepository
    {
        private readonly Room _room = new()
        {
            Id = 1,
            Name = "Room 1",
            TotalRows = 5,
            TotalCols = 5,
            ScreenType = ScreenType.TwoD,
            IsActive = true
        };

        public Room? GetById(int id)
        {
            return id == 1 ? _room : null;
        }

        public IEnumerable<Room> GetAll()
        {
            return new[] { _room };
        }

        public int Insert(Room entity)
        {
            return 1;
        }

        public bool Update(Room entity)
        {
            return true;
        }

        public bool Delete(int id)
        {
            return true;
        }

        public bool IsRoomNameExists(
            string name,
            int? excludeId = null)
        {
            return false;
        }

        public IEnumerable<Room> GetActive()
        {
            return new[] { _room };
        }
    }
}