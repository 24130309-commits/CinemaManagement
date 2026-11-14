using System;
using System.Collections.Generic;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL.Repositories
{
    public class SettingRepository : ISettingRepository
    {
        private readonly SqliteConnectionFactory _factory;
        public SettingRepository(SqliteConnectionFactory factory) => _factory = factory;

        public IEnumerable<AppSetting> GetAll()
        {
            var list = new List<AppSetting>();
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Key, Value, Description FROM AppSettings";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new AppSetting
                {
                    Key = r.GetString(0),
                    Value = r.GetString(1),
                    Description = r.IsDBNull(2) ? null : r.GetString(2)
                });
            }
            return list;
        }

        public AppSetting GetByKey(string key)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Key, Value, Description FROM AppSettings WHERE Key = @k";
            cmd.Parameters.AddWithValue("@k", key);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new AppSetting { Key = r.GetString(0), Value = r.GetString(1), Description = r.IsDBNull(2) ? null : r.GetString(2) };
        }

        public void Upsert(AppSetting setting)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO AppSettings (Key, Value, Description) VALUES (@k, @v, @d)
                                ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value, Description = COALESCE(excluded.Description, AppSettings.Description)";
            cmd.Parameters.AddWithValue("@k", setting.Key);
            cmd.Parameters.AddWithValue("@v", setting.Value);
            cmd.Parameters.AddWithValue("@d", (object)setting.Description ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }
}