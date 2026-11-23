
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly ISeatRepository _seatRepository;

    public RoomService(
        IRoomRepository roomRepository,
        ISeatRepository seatRepository)
    {
        _roomRepository = roomRepository
            ?? throw new ArgumentNullException(nameof(roomRepository));

        _seatRepository = seatRepository
            ?? throw new ArgumentNullException(nameof(seatRepository));
    }

    public IEnumerable<Room> GetAllRooms()
    {
        return _roomRepository.GetAll();
    }

    public Room GetRoomById(int id)
    {
        if (id <= 0)
            throw new BusinessException("Mã phòng chiếu không hợp lệ.");

        return _roomRepository.GetById(id)
            ?? throw new BusinessException("Không tìm thấy phòng chiếu.");
    }

    public void AddRoom(Room room)
    {
        ValidateRoom(room);

        if (_roomRepository.IsRoomNameExists(room.Name))
            throw new BusinessException("Tên phòng chiếu đã tồn tại.");

        if (_roomRepository.Insert(room) <= 0)
            throw new BusinessException("Không thể thêm phòng chiếu.");
    }

    public void UpdateRoom(Room room)
    {
        ValidateRoom(room);

        if (room.Id <= 0)
            throw new BusinessException("Mã phòng chiếu không hợp lệ.");

        if (_roomRepository.IsRoomNameExists(room.Name, room.Id))
            throw new BusinessException("Tên phòng chiếu bị trùng với phòng khác.");

        if (!_roomRepository.Update(room))
            throw new BusinessException("Không thể cập nhật phòng chiếu.");
    }

    public void DeleteRoom(int id)
    {
        if (id <= 0)
            throw new BusinessException("Mã phòng chiếu không hợp lệ.");

        if (!_roomRepository.Delete(id))
            throw new BusinessException("Không thể xóa phòng chiếu.");
    }

    public IEnumerable<Seat> GetSeats(int roomId)
    {
        if (roomId <= 0)
            throw new BusinessException("Mã phòng chiếu không hợp lệ.");

        if (_roomRepository.GetById(roomId) == null)
            throw new BusinessException("Không tìm thấy phòng chiếu.");

        return _seatRepository.GetByRoom(roomId);
    }

    public void UpdateSeatType(int seatId, SeatType type)
    {
        if (seatId <= 0)
            throw new BusinessException("Mã ghế không hợp lệ.");

        if (!Enum.IsDefined(typeof(SeatType), type))
            throw new BusinessException("Loại ghế không hợp lệ.");

        if (!_seatRepository.UpdateType(seatId, type))
            throw new BusinessException("Không thể cập nhật loại ghế.");
    }

    public void GenerateSeats(int roomId, int rows, int cols)
    {
        if (roomId <= 0)
            throw new BusinessException("Mã phòng chiếu không hợp lệ.");

        if (rows <= 0 || cols <= 0)
            throw new BusinessException("Số hàng và số cột phải lớn hơn 0.");

        if (rows > 26)
            throw new BusinessException("Số hàng không được vượt quá 26.");

        var room = _roomRepository.GetById(roomId);

        if (room == null)
            throw new BusinessException("Không tìm thấy phòng chiếu.");

        var seats = new List<Seat>();

        for (var row = 0; row < rows; row++)
        {
            var rowLabel = ((char)('A' + row)).ToString();

            for (var col = 1; col <= cols; col++)
            {
                seats.Add(new Seat
                {
                    RoomId = roomId,
                    RowLabel = rowLabel,
                    ColNumber = col,
                    SeatType = SeatType.Normal
                });
            }
        }

        var affectedRows = _seatRepository.ReplaceByRoom(roomId, seats);

        if (affectedRows < seats.Count)
            throw new BusinessException("Không thể tạo đầy đủ sơ đồ ghế.");
    }

    private static void ValidateRoom(Room room)
    {
        if (room == null)
            throw new BusinessException("Thông tin phòng chiếu không hợp lệ.");

        if (string.IsNullOrWhiteSpace(room.Name))
            throw new BusinessException("Tên phòng chiếu không được để trống.");

        if (room.TotalRows <= 0 || room.TotalCols <= 0)
            throw new BusinessException("Số hàng và số cột ghế phải lớn hơn 0.");
    }
}
