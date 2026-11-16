namespace CinemaManagement.Core.Enums;

public enum SeatStatus
{
    Available = 0,
    Holding = 1,
    Booked = 2,
    Disabled = 3,

    Held = Holding,
    Sold = Booked
}