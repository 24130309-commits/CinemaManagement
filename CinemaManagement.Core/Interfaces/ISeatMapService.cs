using CinemaManagement.Core.DTOs;

namespace CinemaManagement.Core.Interfaces;

public interface ISeatMapService
{
    List<SeatDisplayDto> GetSeatMapByShowtime(int showtimeId);
}