using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly string _connectionString;

        public RoomRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Room> GetAll()
        {
            var list = new List<Room>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Rooms ORDER BY Name ASC";
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

        public Room GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Rooms WHERE Id = @Id";
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

        public bool IsRoomNameExists(string name, int? excludeId = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT COUNT(1) FROM Rooms 
                                 WHERE LOWER(Name) = LOWER(@Name) 
                                 AND (@ExcludeId IS NULL OR Id <> @ExcludeId)";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", name.Trim());
                cmd.Parameters.AddWithValue("@ExcludeId", (object)excludeId ?? DBNull.Value);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public void Add(Room entity)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO Rooms (Name, TotalRows, SeatsPerRow, RoomType, IsActive) 
                                 VALUES (@Name, @TotalRows, @SeatsPerRow, @RoomType, @IsActive)";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", entity.Name);
                cmd.Parameters.AddWithValue("@TotalRows", entity.TotalRows);
                cmd.Parameters.AddWithValue("@SeatsPerRow", entity.SeatsPerRow);
                cmd.Parameters.AddWithValue("@RoomType", (object)entity.RoomType ?? "2D");
                cmd.Parameters.AddWithValue("@IsActive", entity.IsActive);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(Room entity)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE Rooms 
                                 SET Name = @Name, TotalRows = @TotalRows, SeatsPerRow = @SeatsPerRow, 
                                     RoomType = @RoomType, IsActive = @IsActive 
                                 WHERE Id = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", entity.Id);
                cmd.Parameters.AddWithValue("@Name", entity.Name);
                cmd.Parameters.AddWithValue("@TotalRows", entity.TotalRows);
                cmd.Parameters.AddWithValue("@SeatsPerRow", entity.SeatsPerRow);
                cmd.Parameters.AddWithValue("@RoomType", (object)entity.RoomType ?? "2D");
                cmd.Parameters.AddWithValue("@IsActive", entity.IsActive);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Rooms WHERE Id = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private Room MapReader(SqlDataReader reader)
        {
            return new Room
            {
                Id = (int)reader["Id"],
                Name = reader["Name"].ToString(),
                TotalRows = (int)reader["TotalRows"],
                SeatsPerRow = (int)reader["SeatsPerRow"],
                RoomType = reader["RoomType"]?.ToString(),
                IsActive = (bool)reader["IsActive"]
            };
        }
    }
}