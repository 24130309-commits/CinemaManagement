using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace CinemaManagement.BLL.Services;

public class SeatMapService : ISeatMapService
{
    private readonly IShowtimeRepository _showtimeRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly ISeatHoldRepository _seatHoldRepo;
    private readonly string _connectionString;

    public SeatMapService(
        IShowtimeRepository showtimeRepo,
        ITicketRepository ticketRepo,
        ISeatHoldRepository seatHoldRepo,
        string connectionString = "Data Source=cinema.db")
    {
        _showtimeRepo = showtimeRepo;
        _ticketRepo = ticketRepo;
        _seatHoldRepo = seatHoldRepo;
        _connectionString = connectionString;
    }

    public List<SeatDisplayDto> GetSeatMapByShowtime(int showtimeId)
    {
        var showtime = _showtimeRepo.GetById(showtimeId)
            ?? throw new BusinessException(
                "Không tìm thấy suất chiếu.");

        _seatHoldRepo.ReleaseExpiredHolds();

        var soldSeatIds = _ticketRepo
            .GetByShowTimeId(showtimeId)
            .Select(t => t.SeatId)
            .ToHashSet();

        var heldSeatIds = _seatHoldRepo
            .GetActiveHoldsByShowTime(showtimeId)
            .Where(h => !h.IsReleased && h.ExpiryTime > DateTime.Now)
            .Select(h => h.SeatId)
            .ToHashSet();

        var result = new List<SeatDisplayDto>();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, RowLabel, SeatNumber, SeatType
            FROM Seats
            WHERE RoomId = @RoomId
            ORDER BY RowLabel, SeatNumber";

        cmd.Parameters.AddWithValue("@RoomId", showtime.RoomId);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            int seatId = reader.GetInt32(0);
            string rowName = reader.GetString(1);
            int seatNumber = reader.GetInt32(2);

            string seatType = reader.IsDBNull(3)
                ? "Standard"
                : reader.GetString(3);

            var status = SeatStatus.Available;

            if (soldSeatIds.Contains(seatId))
            {
                status = SeatStatus.Booked;
            }
            else if (heldSeatIds.Contains(seatId))
            {
                status = SeatStatus.Holding;
            }

            decimal price = CalculateSeatPrice(
                showtime.BasePrice,
                seatType);

            result.Add(new SeatDisplayDto
            {
                Id = seatId,
                RowName = rowName,
                SeatNumber = seatNumber,
                Status = status,
                Price = price
            });
        }

        return result;
    }

    private static decimal CalculateSeatPrice(
        decimal basePrice,
        string seatType)
    {
        return seatType.ToUpperInvariant() switch
        {
            "VIP" => basePrice + 20000m,
            "COUPLE" => basePrice * 2m,
            _ => basePrice
        };
    }
}