namespace CinemaManagement.Core.DTOS;

public class CustomerPurchaseHistoryDto
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TicketCount { get; set; }
}