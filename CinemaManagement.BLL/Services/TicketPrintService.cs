using System.Text;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public class TicketPrintService : ITicketPrintService
{
    private readonly IBookingRepository _bookingRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly IShowtimeRepository _showtimeRepo;

    public TicketPrintService(
        IBookingRepository bookingRepo,
        ITicketRepository ticketRepo,
        IShowtimeRepository showtimeRepo)
    {
        _bookingRepo = bookingRepo;
        _ticketRepo = ticketRepo;
        _showtimeRepo = showtimeRepo;
    }

    public string GenerateTicketContent(int bookingId)
    {
        var booking = _bookingRepo.GetById(bookingId)
            ?? throw new BusinessException($"Không tìm thấy hóa đơn #{bookingId}.");

        var tickets = _ticketRepo.GetByBookingId(bookingId);
        if (tickets.Count == 0)
            throw new BusinessException("Hóa đơn chưa có vé nào.");

        var showtime = _showtimeRepo.GetById(tickets[0].ShowTimeId);

        var sb = new StringBuilder();
        sb.AppendLine("==========================================");
        sb.AppendLine("           VÉ XEM PHIM - CINEMA           ");
        sb.AppendLine("==========================================");
        sb.AppendLine($"Mã hóa đơn  : #{booking.Id}");
        sb.AppendLine($"Thời gian   : {booking.BookingTime:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Phim        : {showtime?.MovieTitle ?? "N/A"}");
        sb.AppendLine($"Phòng chiếu : {showtime?.RoomName ?? "N/A"}");
        if (showtime != null)
            sb.AppendLine($"Suất chiếu  : {showtime.StartTime:dd/MM/yyyy HH:mm}");
        sb.AppendLine("------------------------------------------");
        sb.AppendLine("Danh sách vé:");
        foreach (var t in tickets)
        {
            sb.AppendLine($"  - Vé #{t.Id} | Mã ghế: {t.SeatId} | Giá: {t.Price:N0} VNĐ");
        }
        sb.AppendLine("------------------------------------------");
        sb.AppendLine($"Thanh toán  : {booking.PaymentMethod}");
        sb.AppendLine($"Tổng tiền   : {booking.TotalAmount:N0} VNĐ");
        sb.AppendLine("==========================================");
        sb.AppendLine("     Cảm ơn quý khách & Chúc xem phim vui! ");

        return sb.ToString();
    }

    public void ExportTicketToPdf(int bookingId, string filePath)
    {
        string content = GenerateTicketContent(bookingId);
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }
}