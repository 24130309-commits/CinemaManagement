using System;
using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces
{
    public interface IShowtimesService
    {
        IEnumerable<Showtime> GetAllShowtimes();
        IEnumerable<Showtime> GetShowtimesByDate(DateTime date);
        Showtime GetShowtimeById(int id);
        void AddShowtime(Showtime showtime);
        void UpdateShowtime(Showtime showtime);
        void DeleteShowtime(int id);
    }
}