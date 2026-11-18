namespace CinemaManagement.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }

    // Giữ kiểu int của A để không phá vỡ code hiện tại.
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int? EntityId { get; set; }

    // A đang sử dụng Detail, B đang sử dụng Details.
    // Hai tên cùng trỏ đến một nội dung để tương thích.
    public string Detail { get; set; } = string.Empty;

    public string Details
    {
        get => Detail;
        set => Detail = value;
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}