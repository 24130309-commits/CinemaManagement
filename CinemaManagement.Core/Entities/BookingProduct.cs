namespace CinemaManagement.Core.Entities;

public class BookingProduct
{
    // Bảng SQLite dùng khóa chính kép BookingId + ProductId.
    public int BookingId { get; set; }
    public int ProductId { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }

    // Tương thích code cũ của B.
    public int Quantity
    {
        get => Qty;
        set => Qty = value;
    }

    public decimal Price
    {
        get => UnitPrice;
        set => UnitPrice = value;
    }
}