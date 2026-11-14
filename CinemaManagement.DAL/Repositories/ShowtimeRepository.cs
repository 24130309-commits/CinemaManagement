using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL.Repositories
{
    public class ShowtimeRepository : IShowtimeRepository
    {
        private readonly string _connectionString;

        public ShowtimeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Showtime> GetAll()
        {
            var list = new List<Showtime>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT s.*, m.Title AS MovieTitle, r.Name AS RoomName 
                                 FROM Showtimes s
                                 INNER JOIN Movies m ON s.MovieId = m.Id
                                 INNER JOIN Rooms r ON s.RoomId = r.Id
                                 ORDER BY s.StartTime DESC";
                var cmd = new SqlCommand(query, conn);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapReaderToShowtime(reader));
                    }
                }
            }
            return list;
        }

        public IEnumerable<Showtime> GetByDate(DateTime date)
        {
            var list = new List<Showtime>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT s.*, m.Title AS MovieTitle, r.Name AS RoomName 
                                 FROM Showtimes s
                                 INNER JOIN Movies m ON s.MovieId = m.Id
                                 INNER JOIN Rooms r ON s.RoomId = r.Id
                                 WHERE CAST(s.StartTime AS DATE) = @Date
                                 ORDER BY s.StartTime ASC";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Date", date.Date);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapReaderToShowtime(reader));
                    }
                }
            }
            return list;
        }

        public IEnumerable<Showtime> GetByMovieId(int movieId)
        {
            var list = new List<Showtime>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT s.*, m.Title AS MovieTitle, r.Name AS RoomName 
                                 FROM Showtimes s
                                 INNER JOIN Movies m ON s.MovieId = m.Id
                                 INNER JOIN Rooms r ON s.RoomId = r.Id
                                 WHERE s.MovieId = @MovieId";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@MovieId", movieId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapReaderToShowtime(reader));
                    }
                }
            }
            return list;
        }

        public Showtime GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT s.*, m.Title AS MovieTitle, r.Name AS RoomName 
                                 FROM Showtimes s
                                 INNER JOIN Movies m ON s.MovieId = m.Id
                                 INNER JOIN Rooms r ON s.RoomId = r.Id
                                 WHERE s.Id = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapReaderToShowtime(reader);
                }
            }
            return null;
        }

        public bool HasConflict(int roomId, DateTime startTime, DateTime endTime, int? excludeShowtimeId = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT COUNT(1) FROM Showtimes 
                                 WHERE RoomId = @RoomId 
                                 AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
                                 AND (StartTime < @EndTime AND EndTime > @StartTime)";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@RoomId", roomId);
                cmd.Parameters.AddWithValue("@StartTime", startTime);
                cmd.Parameters.AddWithValue("@EndTime", endTime);
                cmd.Parameters.AddWithValue("@ExcludeId", (object)excludeShowtimeId ?? DBNull.Value);

                conn.Open();
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        public void Add(Showtime entity)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO Showtimes (MovieId, RoomId, StartTime, EndTime, BasePrice) 
                                 VALUES (@MovieId, @RoomId, @StartTime, @EndTime, @BasePrice)";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@MovieId", entity.MovieId);
                cmd.Parameters.AddWithValue("@RoomId", entity.RoomId);
                cmd.Parameters.AddWithValue("@StartTime", entity.StartTime);
                cmd.Parameters.AddWithValue("@EndTime", entity.EndTime);
                cmd.Parameters.AddWithValue("@BasePrice", entity.BasePrice);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(Showtime entity)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE Showtimes 
                                 SET MovieId = @MovieId, RoomId = @RoomId, StartTime = @StartTime, 
                                     EndTime = @EndTime, BasePrice = @BasePrice 
                                 WHERE Id = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", entity.Id);
                cmd.Parameters.AddWithValue("@MovieId", entity.MovieId);
                cmd.Parameters.AddWithValue("@RoomId", entity.RoomId);
                cmd.Parameters.AddWithValue("@StartTime", entity.StartTime);
                cmd.Parameters.AddWithValue("@EndTime", entity.EndTime);
                cmd.Parameters.AddWithValue("@BasePrice", entity.BasePrice);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Showtimes WHERE Id = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private Showtime MapReaderToShowtime(SqlDataReader reader)
        {
            return new Showtime
            {
                Id = (int)reader["Id"],
                MovieId = (int)reader["MovieId"],
                RoomId = (int)reader["RoomId"],
                StartTime = (DateTime)reader["StartTime"],
                EndTime = (DateTime)reader["EndTime"],
                BasePrice = (decimal)reader["BasePrice"],
                MovieTitle = reader["MovieTitle"]?.ToString(),
                RoomName = reader["RoomName"]?.ToString()
            };
        }
    }
}