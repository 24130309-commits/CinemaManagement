using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Entities;

public class Booking
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public BookingStatus Status { get; set; }

    // Tương thích một phần với code cũ của B.
    public DateTime BookingTime
    {
        get => CreatedAt;
        set => CreatedAt = value;
    }

    public decimal TotalAmount
    {
        get => Total;
        set => Total = value;
    }
}