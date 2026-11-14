using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL.Repositories
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly string _connectionString;

        public PromotionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Promotion> GetAll()
        {
            var list = new List<Promotion>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Promotions ORDER BY Id DESC";
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

        public Promotion GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Promotions WHERE Id = @Id";
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

        public Promotion GetByCode(string code)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Promotions WHERE UPPER(Code) = UPPER(@Code)";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Code", code.Trim());
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapReader(reader);
                }
            }
            return null;
        }

        public bool IsCodeExists(string code, int? excludeId = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT COUNT(1) FROM Promotions 
                                 WHERE UPPER(Code) = UPPER(@Code) 
                                 AND (@ExcludeId IS NULL OR Id <> @ExcludeId)";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Code", code.Trim());
                cmd.Parameters.AddWithValue("@ExcludeId", (object)excludeId ?? DBNull.Value);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public void Add(Promotion entity)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO Promotions (Code, Description, DiscountPercent, MaxDiscountAmount, StartDate, EndDate, IsActive) 
                                 VALUES (@Code, @Description, @DiscountPercent, @MaxDiscountAmount, @StartDate, @EndDate, @IsActive)";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Code", entity.Code);
                cmd.Parameters.AddWithValue("@Description", (object)entity.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DiscountPercent", entity.DiscountPercent);
                cmd.Parameters.AddWithValue("@MaxDiscountAmount", entity.MaxDiscountAmount);
                cmd.Parameters.AddWithValue("@StartDate", entity.StartDate);
                cmd.Parameters.AddWithValue("@EndDate", entity.EndDate);
                cmd.Parameters.AddWithValue("@IsActive", entity.IsActive);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(Promotion entity)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE Promotions 
                                 SET Code = @Code, Description = @Description, DiscountPercent = @DiscountPercent, 
                                     MaxDiscountAmount = @MaxDiscountAmount, StartDate = @StartDate, 
                                     EndDate = @EndDate, IsActive = @IsActive 
                                 WHERE Id = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", entity.Id);
                cmd.Parameters.AddWithValue("@Code", entity.Code);
                cmd.Parameters.AddWithValue("@Description", (object)entity.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DiscountPercent", entity.DiscountPercent);
                cmd.Parameters.AddWithValue("@MaxDiscountAmount", entity.MaxDiscountAmount);
                cmd.Parameters.AddWithValue("@StartDate", entity.StartDate);
                cmd.Parameters.AddWithValue("@EndDate", entity.EndDate);
                cmd.Parameters.AddWithValue("@IsActive", entity.IsActive);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Promotions WHERE Id = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private Promotion MapReader(SqlDataReader reader)
        {
            return new Promotion
            {
                Id = (int)reader["Id"],
                Code = reader["Code"].ToString(),
                Description = reader["Description"]?.ToString(),
                DiscountPercent = (decimal)reader["DiscountPercent"],
                MaxDiscountAmount = (decimal)reader["MaxDiscountAmount"],
                StartDate = (DateTime)reader["StartDate"],
                EndDate = (DateTime)reader["EndDate"],
                IsActive = (bool)reader["IsActive"]
            };
        }
    }
}