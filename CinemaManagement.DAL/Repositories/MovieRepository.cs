using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL.Repositories
{
    public class MovieRepository : IMovieRepository
    {
        private readonly string _connectionString;

        public MovieRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Movie> GetAll()
        {
            var list = new List<Movie>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Movies ORDER BY Id DESC";
                var cmd = new SqlCommand(query, conn);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapReader(reader));
                }
            }
            return list;
        }

        public Movie GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Movies WHERE Id = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapReader(reader);
                }
            }
            return null;
        }

        public IEnumerable<Movie> GetMoviesByStatus(MovieStatus status)
        {
            var list = new List<Movie>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Movies WHERE Status = @Status";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Status", (int)status);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapReader(reader));
                }
            }
            return list;
        }

        public bool IsTitleExists(string title, int? excludeId = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT COUNT(1) FROM Movies 
                                 WHERE LOWER(Title) = LOWER(@Title) 
                                 AND (@ExcludeId IS NULL OR Id <> @ExcludeId)";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Title", title.Trim());
                cmd.Parameters.AddWithValue("@ExcludeId", (object)excludeId ?? DBNull.Value);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public void Add(Movie entity)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO Movies (Title, Description, Duration, ReleaseDate, Status) 
                                 VALUES (@Title, @Description, @Duration, @ReleaseDate, @Status)";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Title", entity.Title);
                cmd.Parameters.AddWithValue("@Description", (object)entity.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Duration", entity.Duration);
                cmd.Parameters.AddWithValue("@ReleaseDate", entity.ReleaseDate);
                cmd.Parameters.AddWithValue("@Status", (int)entity.Status);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(Movie entity)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE Movies 
                                 SET Title = @Title, Description = @Description, Duration = @Duration, 
                                     ReleaseDate = @ReleaseDate, Status = @Status 
                                 WHERE Id = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", entity.Id);
                cmd.Parameters.AddWithValue("@Title", entity.Title);
                cmd.Parameters.AddWithValue("@Description", (object)entity.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Duration", entity.Duration);
                cmd.Parameters.AddWithValue("@ReleaseDate", entity.ReleaseDate);
                cmd.Parameters.AddWithValue("@Status", (int)entity.Status);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Movies WHERE Id = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private Movie MapReader(SqlDataReader reader)
        {
            return new Movie
            {
                Id = (int)reader["Id"],
                Title = reader["Title"].ToString(),
                Description = reader["Description"]?.ToString(),
                Duration = (int)reader["Duration"],
                ReleaseDate = (DateTime)reader["ReleaseDate"],
                Status = (MovieStatus)(int)reader["Status"]
            };
        }
    }
}