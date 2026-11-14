using System.Collections.Generic;
using System.Data.SqlClient;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL.Repositories
{
    public class PricingRuleRepository : IPricingRuleRepository
    {
        private readonly string _connectionString;

        public PricingRuleRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<PricingRule> GetAll()
        {
            var list = new List<PricingRule>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM PricingRules ORDER BY Id ASC";
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

        public PricingRule GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM PricingRules WHERE Id = @Id";
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

        public IEnumerable<PricingRule> GetByDayAndSeatType(string dayType, string seatType)
        {
            var list = new List<PricingRule>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT * FROM PricingRules 
                                 WHERE (DayType = @DayType OR DayType = 'All') 
                                 AND (SeatType = @SeatType OR SeatType = 'All')";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DayType", dayType);
                cmd.Parameters.AddWithValue("@SeatType", seatType);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapReader(reader));
                }
            }
            return list;
        }

        public void Add(PricingRule entity)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO PricingRules (Name, DayType, SeatType, ExtraFee) 
                                 VALUES (@Name, @DayType, @SeatType, @ExtraFee)";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", entity.Name);
                cmd.Parameters.AddWithValue("@DayType", entity.DayType);
                cmd.Parameters.AddWithValue("@SeatType", entity.SeatType);
                cmd.Parameters.AddWithValue("@ExtraFee", entity.ExtraFee);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(PricingRule entity)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE PricingRules 
                                 SET Name = @Name, DayType = @DayType, SeatType = @SeatType, ExtraFee = @ExtraFee 
                                 WHERE Id = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", entity.Id);
                cmd.Parameters.AddWithValue("@Name", entity.Name);
                cmd.Parameters.AddWithValue("@DayType", entity.DayType);
                cmd.Parameters.AddWithValue("@SeatType", entity.SeatType);
                cmd.Parameters.AddWithValue("@ExtraFee", entity.ExtraFee);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM PricingRules WHERE Id = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private PricingRule MapReader(SqlDataReader reader)
        {
            return new PricingRule
            {
                Id = (int)reader["Id"],
                Name = reader["Name"].ToString(),
                DayType = reader["DayType"].ToString(),
                SeatType = reader["SeatType"].ToString(),
                ExtraFee = (decimal)reader["ExtraFee"]
            };
        }
    }
}