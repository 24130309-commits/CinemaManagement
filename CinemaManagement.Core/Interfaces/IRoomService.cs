using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Interfaces;

public interface IRoomService
{
    IEnumerable<Room> GetAllRooms();

    Room GetRoomById(int id);

    void AddRoom(Room room);

    void UpdateRoom(Room room);

    void DeleteRoom(int id);

    IEnumerable<Seat> GetSeats(int roomId);

    void UpdateSeatType(int seatId, SeatType type);

    void UpdateSeatTypes(IEnumerable<Seat> seats);

    void GenerateSeats(int roomId, int rows, int cols);
}