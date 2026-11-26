using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Interfaces;

public interface ISeatRepository
{
    IEnumerable<Seat> GetByRoom(int roomId);

    int InsertBatch(IEnumerable<Seat> seats);

    int ReplaceByRoom(
        int roomId,
        IEnumerable<Seat> seats,
        int totalRows,
        int totalCols);

    bool DeleteByRoom(int roomId);

    bool UpdateType(int seatId, SeatType type);

    int UpdateTypes(IEnumerable<Seat> seats);
}