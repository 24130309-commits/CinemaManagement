using CinemaManagement.Core.DTOS;
namespace CinemaManagement.Core.Interfaces;
public interface ISeatMapService
{
    List<SeatDisplayDto> GetSeatMapByShowtime(int showtimeId);
    
}