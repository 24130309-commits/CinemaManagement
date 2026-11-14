using System;
using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces
{
    public interface IShowtimeRepository : IRepository<Showtime>
    {
        IEnumerable<Showtime> GetByMovieId(int movieId);
        IEnumerable<Showtime> GetByDate(DateTime date);
        bool HasConflict(int roomId, DateTime startTime, DateTime endTime, int? excludeShowtimeId = null);
    }
}