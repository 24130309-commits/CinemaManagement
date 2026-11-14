using System;
using System.Collections.Generic;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly SqliteConnectionFactory _factory;
        public AuditLogRepository(SqliteConnectionFactory factory) => _factory = factory;

        public IEnumerable<AuditLog> GetAll() => GetRecentLogs(200);
        public AuditLog GetById(int id) => null;
        public void Update(AuditLog entity) { }
        public void Delete(int id) { }

        public IEnumerable<AuditLog> GetRecentLogs(int limit = 100)
        {
            var list = new List<AuditLog>();
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT a.Id, a.UserId, u.Username, a.Action, a.EntityName, a.EntityId, a.Details, a.CreatedAt
                                FROM AuditLogs a LEFT JOIN Users u ON a.UserId = u.Id
                                ORDER BY a.Id DESC LIMIT @limit";
            cmd.Parameters.AddWithValue("@limit", limit);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new AuditLog
                {
                    Id = r.GetInt32(0),
                    UserId = r.IsDBNull(1) ? (int?)null : r.GetInt32(1),
                    Username = r.IsDBNull(2) ? "System" : r.GetString(2),
                    Action = r.GetString(3),
                    EntityName = r.IsDBNull(4) ? null : r.GetString(4),
                    EntityId = r.IsDBNull(5) ? (int?)null : r.GetInt32(5),
                    Details = r.IsDBNull(6) ? null : r.GetString(6),
                    CreatedAt = DateTime.TryParse(r.GetString(7), out var dt) ? dt : DateTime.Now
                });
            }
            return list;
        }

        public void Add(AuditLog entity)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, Details)
                                VALUES (@uid, @act, @ename, @eid, @det)";
            cmd.Parameters.AddWithValue("@uid", (object)entity.UserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@act", entity.Action);
            cmd.Parameters.AddWithValue("@ename", (object)entity.EntityName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@eid", (object)entity.EntityId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@det", (object)entity.Details ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }
}