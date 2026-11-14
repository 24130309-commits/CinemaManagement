namespace CinemaManagement.Core.Exceptions;

public class SeatUnavailbleExcption : Exception
{
    public SeatUnavailbleExcption()
        : base ("Ghe nay da duoc giu hoac da duoc dat boi nguoi khac."){}
    
    public  SeatUnavailbleExcption(string message)
        : base(message){}
}