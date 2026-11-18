namespace CinemaManagement.Core.Exceptions;

public class ShowtimeConflictException : Exception
{
    public ShowtimeConflictException()
    {
    }

    public ShowtimeConflictException(string message)
        : base(message)
    {
    }

    public ShowtimeConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}