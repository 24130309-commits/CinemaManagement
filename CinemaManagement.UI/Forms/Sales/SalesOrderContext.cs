using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.UI.Forms.Sales;

public sealed class SalesOrderContext
{
    public Showtime? Showtime { get; set; }
    public Customer? Customer { get; set; }
    public List<int> SelectedSeatIds { get; } = new();
    public Dictionary<int, int> HoldIds { get; } = new();
    public Dictionary<int, DateTime> HoldExpiresAt { get; } = new();
    public List<BookingProduct> Products { get; } = new();
    public Dictionary<int, string> ProductNames { get; } = new();
    public int BookingId { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public decimal Discount { get; set; }

    public bool HasSelectedSeats => SelectedSeatIds.Count > 0;

    public decimal ProductTotal => Products.Sum(item => item.Price * item.Quantity);

    public void Reset()
    {
        Showtime = null;
        Customer = null;
        SelectedSeatIds.Clear();
        HoldIds.Clear();
        HoldExpiresAt.Clear();
        Products.Clear();
        ProductNames.Clear();
        BookingId = 0;
        PromotionCode = string.Empty;
        Discount = 0;
    }
}
