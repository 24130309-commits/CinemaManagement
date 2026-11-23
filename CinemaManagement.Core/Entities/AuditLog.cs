namespace CinemaManagement.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    public string Detail { get; set; } = string.Empty;

    public string Details
    {
        get => Detail;
        set => Detail = value;
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}