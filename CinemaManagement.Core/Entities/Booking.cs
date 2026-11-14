namespace CinemaManagement.Core.Entities;

public class Booking
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime BookingTime { get; set; }
    public Decimal TotalAmount { get; set; }
    public string Status  { get; set; }
    public string PaymentMethod  { get; set; }
}