using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Entities;

public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public string TransactionCode { get; set; } = string.Empty;

    public PaymentMethod PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public DateTime PaidAt { get; set; }

    public string ProviderReference { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}