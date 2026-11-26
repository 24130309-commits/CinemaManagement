using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.BLL.Session;

namespace CinemaManagement.BLL.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly CurrentUserSession _session;

    public RoomService(
        IRoomRepository roomRepository,
        ISeatRepository seatRepository,
        CurrentUserSession session)
    {
        _roomRepository = roomRepository
            ?? throw new ArgumentNullException(nameof(roomRepository));

        _seatRepository = seatRepository
            ?? throw new ArgumentNullException(nameof(seatRepository));
        _session = session
            ?? throw new ArgumentNullException(nameof(session));
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
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý phòng chiếu");
        ValidateRoom(room);

        if (_roomRepository.IsRoomNameExists(room.Name))
            throw new BusinessException("Tên phòng chiếu đã tồn tại.");

        if (_roomRepository.Insert(room) <= 0)
            throw new BusinessException("Không thể thêm phòng chiếu.");
    }

    public void UpdateRoom(Room room)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý phòng chiếu");
        ValidateRoom(room);

        if (room.Id <= 0)
            throw new BusinessException("Mã phòng chiếu không hợp lệ.");

        var existingRoom = _roomRepository.GetById(room.Id)
            ?? throw new BusinessException("Không tìm thấy phòng chiếu.");

        if (_roomRepository.IsRoomNameExists(room.Name, room.Id))
            throw new BusinessException("Tên phòng chiếu bị trùng với phòng khác.");

        var hasSeats = _seatRepository.GetByRoom(room.Id).Any();

        var dimensionsChanged =
            room.TotalRows != existingRoom.TotalRows ||
            room.TotalCols != existingRoom.TotalCols;

        if (hasSeats && dimensionsChanged)
        {
            throw new BusinessException(
                "Phòng đã có sơ đồ ghế nên không thể thay đổi số hàng hoặc số cột tại màn hình quản lý phòng. Vui lòng sử dụng chức năng Thiết kế sơ đồ ghế.");
        }

        if (!_roomRepository.Update(room))
            throw new BusinessException("Không thể cập nhật phòng chiếu.");
    }

    public void DeleteRoom(int id)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý phòng chiếu");
        if (id <= 0)
            throw new BusinessException("Mã phòng chiếu không hợp lệ.");

        if (_roomRepository.GetById(id) is null)
            throw new BusinessException("Không tìm thấy phòng chiếu.");

        if (_roomRepository.HasShowtimes(id))
        {
            throw new BusinessException(
                "Không thể xóa phòng chiếu vì phòng đã có lịch sử suất chiếu. Bạn có thể chuyển phòng sang trạng thái ngừng hoạt động.");
        }

        if (!_roomRepository.Delete(id))
            throw new BusinessException("Không thể xóa phòng chiếu.");
    }

    public IEnumerable<Seat> GetSeats(int roomId)
    {
        if (roomId <= 0)
            throw new BusinessException("Mã phòng chiếu không hợp lệ.");

        if (_roomRepository.GetById(roomId) is null)
            throw new BusinessException("Không tìm thấy phòng chiếu.");

        return _seatRepository.GetByRoom(roomId);
    }

    public void UpdateSeatType(int seatId, SeatType type)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý sơ đồ ghế");
        if (seatId <= 0)
            throw new BusinessException("Mã ghế không hợp lệ.");

        if (!Enum.IsDefined(typeof(SeatType), type))
            throw new BusinessException("Loại ghế không hợp lệ.");

        if (!_seatRepository.UpdateType(seatId, type))
            throw new BusinessException("Không thể cập nhật loại ghế.");
    }

    public void UpdateSeatTypes(IEnumerable<Seat> seats)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý sơ đồ ghế");
        if (seats is null)
            throw new BusinessException("Danh sách ghế không hợp lệ.");

        var seatList = seats.ToList();

        if (seatList.Count == 0)
            return;

        foreach (var seat in seatList)
        {
            if (seat.Id <= 0)
                throw new BusinessException("Mã ghế không hợp lệ.");

            if (!Enum.IsDefined(typeof(SeatType), seat.SeatType))
                throw new BusinessException("Loại ghế không hợp lệ.");
        }

        var affectedRows = _seatRepository.UpdateTypes(seatList);

        if (affectedRows != seatList.Count)
        {
            throw new BusinessException(
                "Không thể cập nhật đầy đủ loại ghế. Vui lòng kiểm tra lại sơ đồ ghế.");
        }
    }

    public void GenerateSeats(int roomId, int rows, int cols)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý sơ đồ ghế");
        if (roomId <= 0)
            throw new BusinessException("Mã phòng chiếu không hợp lệ.");

        if (rows <= 0 || cols <= 0)
            throw new BusinessException("Số hàng và số cột phải lớn hơn 0.");

        if (rows > 26)
            throw new BusinessException("Số hàng không được vượt quá 26.");

        if (cols > 40)
            throw new BusinessException("Số cột không được vượt quá 40.");

        _ = _roomRepository.GetById(roomId)
            ?? throw new BusinessException("Không tìm thấy phòng chiếu.");

        var seats = new List<Seat>(rows * cols);

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

        try
        {
            var affectedRows = _seatRepository.ReplaceByRoom(
                roomId,
                seats,
                rows,
                cols);

            if (affectedRows != seats.Count)
            {
                throw new BusinessException(
                    "Không thể tạo đầy đủ sơ đồ ghế.");
            }
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessException(ex.Message);
        }
    }

    private static void ValidateRoom(Room room)
    {
        if (room is null)
            throw new BusinessException("Thông tin phòng chiếu không hợp lệ.");

        if (string.IsNullOrWhiteSpace(room.Name))
            throw new BusinessException("Tên phòng chiếu không được để trống.");

        if (room.TotalRows <= 0 || room.TotalCols <= 0)
            throw new BusinessException("Số hàng và số cột ghế phải lớn hơn 0.");

        if (room.TotalRows > 26)
            throw new BusinessException("Số hàng ghế không được vượt quá 26.");

        if (room.TotalCols > 40)
            throw new BusinessException("Số cột ghế không được vượt quá 40.");

        if (!Enum.IsDefined(typeof(ScreenType), room.ScreenType))
            throw new BusinessException("Loại màn hình không hợp lệ.");
    }
}
