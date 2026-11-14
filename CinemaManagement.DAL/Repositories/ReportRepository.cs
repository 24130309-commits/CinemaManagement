using System;
using System.Collections.Generic;
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly SqliteConnectionFactory _factory;
        public ReportRepository(SqliteConnectionFactory factory) => _factory = factory;

        public IEnumerable<RevenueByMovieDto> GetRevenueByMovie(DateTime fromDate, DateTime toDate)
        {
            var list = new List<RevenueByMovieDto>();
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT m.Id, m.Title, COUNT(DISTINCT s.Id), COUNT(t.Id), COALESCE(SUM(t.FinalPrice), 0)
                FROM Movies m
                LEFT JOIN Showtimes s ON s.MovieId = m.Id AND s.StartTime BETWEEN @from AND @to
                LEFT JOIN Tickets t ON t.ShowtimeId = s.Id
                GROUP BY m.Id, m.Title";
            cmd.Parameters.AddWithValue("@from", fromDate.ToString("yyyy-MM-dd 00:00:00"));
            cmd.Parameters.AddWithValue("@to", toDate.ToString("yyyy-MM-dd 23:59:59"));
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new RevenueByMovieDto
                {
                    MovieId = r.GetInt32(0),
                    MovieTitle = r.GetString(1),
                    TotalShowtimes = r.GetInt32(2),
                    TotalTicketsSold = r.GetInt32(3),
                    TotalRevenue = Convert.ToDecimal(r.GetDouble(4))
                });
            }
            return list;
        }

        public IEnumerable<OccupancyDto> GetOccupancyReport(DateTime fromDate, DateTime toDate)
        {
            var list = new List<OccupancyDto>();
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT s.Id, m.Title, r.Name, s.StartTime, COUNT(t.Id), r.Capacity
                FROM Showtimes s
                JOIN Movies m ON s.MovieId = m.Id
                JOIN Rooms r ON s.RoomId = r.Id
                LEFT JOIN Tickets t ON t.ShowtimeId = s.Id
                WHERE s.StartTime BETWEEN @from AND @to
                GROUP BY s.Id, m.Title, r.Name, s.StartTime, r.Capacity";
            cmd.Parameters.AddWithValue("@from", fromDate.ToString("yyyy-MM-dd 00:00:00"));
            cmd.Parameters.AddWithValue("@to", toDate.ToString("yyyy-MM-dd 23:59:59"));
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new OccupancyDto
                {
                    ShowtimeId = r.GetInt32(0),
                    MovieTitle = r.GetString(1),
                    RoomName = r.GetString(2),
                    StartTime = DateTime.Parse(r.GetString(3)),
                    SoldSeats = r.GetInt32(4),
                    TotalSeats = r.GetInt32(5)
                });
            }
            return list;
        }
    }
}