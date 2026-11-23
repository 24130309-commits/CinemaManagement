namespace CinemaManagement.Core.Entities;

public class BookingProduct
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public int ProductId { get; set; }

    public int Qty { get; set; }

    public decimal UnitPrice { get; set; }

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