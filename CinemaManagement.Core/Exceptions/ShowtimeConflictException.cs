using System;

namespace CinemaManagement.Core.Exceptions
{
    public class ShowtimeConflictException : Exception
    {
        public ShowtimeConflictException(string message) : base(message) { }
    }
}