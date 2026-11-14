using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces
{
    public interface IRoomRepository : IRepository<Room>
    {
        bool IsRoomNameExists(string name, int? excludeId = null);
    }
}