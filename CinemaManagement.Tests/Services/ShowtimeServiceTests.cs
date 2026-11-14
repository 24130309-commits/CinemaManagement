using Xunit;
using System;
using System.Collections.Generic;
using CinemaManagement.BLL.Services;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services
{
    public class ShowtimesServiceTests
    {
        [Fact]
        public void AddShowtime_WhenConflict_ThrowsShowtimeConflictException()
        {
            var showtimeRepo = new StubShowtimeRepository { HasConflictResult = true };
            var movieRepo = new StubMovieRepository();
            var roomRepo = new StubRoomRepository();
            var service = new ShowtimeService(showtimeRepo, movieRepo, roomRepo);

            Assert.Throws<ShowtimeConflictException>(() =>
                service.AddShowtime(new Showtime
                {
                    MovieId = 1,
                    RoomId = 1,
                    StartTime = DateTime.Now.AddHours(2),
                    BasePrice = 80000m
                })
            );
        }

        private class StubShowtimeRepository : IShowtimeRepository
        {
            public bool HasConflictResult { get; set; }
            public bool HasConflict(int roomId, DateTime startTime, DateTime endTime, int? excludeId = null) => HasConflictResult;
            public IEnumerable<Showtime> GetByMovieId(int movieId) => new List<Showtime>();
            public IEnumerable<Showtime> GetByDate(DateTime date) => new List<Showtime>();
            public IEnumerable<Showtime> GetByMovie(int movieId) => new List<Showtime>();
            public IEnumerable<Showtime> GetAll() => new List<Showtime>();
            public Showtime GetById(int id) => null;
            public void Add(Showtime entity) { }
            public void Update(Showtime entity) { }
            public void Delete(int id) { }
        }

        private class StubMovieRepository : IMovieRepository
        {
            public IEnumerable<Movie> GetMoviesByStatus(MovieStatus status) => new List<Movie>();
            public bool IsTitleExists(string title, int? excludeId = null) => false;
            public IEnumerable<Movie> GetAll() => new List<Movie>();
            public Movie GetById(int id) => new Movie { Id = 1, Title = "Test", Duration = 120 };
            public void Add(Movie entity) { }
            public void Update(Movie entity) { }
            public void Delete(int id) { }
        }

        private class StubRoomRepository : IRoomRepository
        {
            public bool IsRoomNameExists(string name, int? excludeId = null) => false;
            public IEnumerable<Room> GetAll() => new List<Room>();
            public Room GetById(int id) => new Room { Id = 1, Name = "Room 1", IsActive = true };
            public void Add(Room entity) { }
            public void Update(Room entity) { }
            public void Delete(int id) { }
        }
    }
}