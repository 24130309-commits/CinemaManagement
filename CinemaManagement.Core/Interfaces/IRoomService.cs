using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces
{
    public interface IRoomService
    {
        IEnumerable<Room> GetAllRooms();
        Room GetRoomById(int id);
        void AddRoom(Room room);
        void UpdateRoom(Room room);
        void DeleteRoom(int id);
    }
}