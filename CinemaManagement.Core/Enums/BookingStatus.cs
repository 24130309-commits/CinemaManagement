namespace CinemaManagement.Core.Enums;

public enum BookingStatus
{
    Pending = 0,
    Paid = 1,
    Cancelled = 2,
    Refunded = 3,

    // Giá trị tương thích với tên cũ.
    Completed = Paid,
    Cancled = Cancelled
}