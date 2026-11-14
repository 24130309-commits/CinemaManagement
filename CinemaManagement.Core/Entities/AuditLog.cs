// CinemaManagement.Core/Entities/AuditLog.cs
using System;

namespace CinemaManagement.Core.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public string EntityName { get; set; }
        public int? EntityId { get; set; }
        public string Details { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

// CinemaManagement.Core/Entities/AppSetting.cs
namespace CinemaManagement.Core.Entities
{
    public partial class AppSetting
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }
}