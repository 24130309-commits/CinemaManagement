using System;
namespace CinemaManagement.Core.Entities;

public class AuditLog
{
   public int Id { get; set; }
   public int UserId { get; set; }
   public string Action { get; set; } = string.Empty;
   public string Details { get; set; } = string.Empty;
   public DateTime CreatedOn { get; set; } =    DateTime.Now;

}