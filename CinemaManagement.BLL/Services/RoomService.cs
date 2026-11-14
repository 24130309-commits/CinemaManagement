using System.Collections.Generic;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;

        public RoomService(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        public IEnumerable<Room> GetAllRooms() => _roomRepository.GetAll();

        public Room GetRoomById(int id) => _roomRepository.GetById(id);

        public void AddRoom(Room room)
        {
            ValidateRoom(room);
            if (_roomRepository.IsRoomNameExists(room.Name))
                throw new BusinessException("Tên phòng chiếu đã tồn tại.");

            _roomRepository.Add(room);
        }

        public void UpdateRoom(Room room)
        {
            ValidateRoom(room);
            if (_roomRepository.IsRoomNameExists(room.Name, room.Id))
                throw new BusinessException("Tên phòng chiếu bị trùng với phòng khác.");

            _roomRepository.Update(room);
        }

        public void DeleteRoom(int id) => _roomRepository.Delete(id);

        private void ValidateRoom(Room room)
        {
            if (room == null)
                throw new BusinessException("Thông tin phòng chiếu không hợp lệ.");
            if (string.IsNullOrWhiteSpace(room.Name))
                throw new BusinessException("Tên phòng chiếu không được để trống.");
            if (room.TotalRows <= 0 || room.SeatsPerRow <= 0)
                throw new BusinessException("Số hàng và số ghế mỗi hàng phải lớn hơn 0.");
        }
    }
}