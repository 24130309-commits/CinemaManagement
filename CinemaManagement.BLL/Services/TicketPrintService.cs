
using System.Globalization;
using System.Text;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CinemaManagement.Core.Enums;
using CinemaManagement.BLL.Session;

namespace CinemaManagement.BLL.Services;

public sealed class TicketPrintService : ITicketPrintService
{
    private readonly IBookingRepository _bookings;
    private readonly ITicketRepository _tickets;
    private readonly IShowtimeRepository _showtimes;
    private readonly ISeatRepository _seats;
    private readonly IProductService _products;
    private readonly CurrentUserSession _session;

    public TicketPrintService(
        IBookingRepository bookings,
        ITicketRepository tickets,
        IShowtimeRepository showtimes,
        ISeatRepository seats,
        IProductService products,
        CurrentUserSession session)
    {
        _bookings = bookings ?? throw new ArgumentNullException(nameof(bookings));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _showtimes = showtimes ?? throw new ArgumentNullException(nameof(showtimes));
        _seats = seats ?? throw new ArgumentNullException(nameof(seats));
        _products = products ?? throw new ArgumentNullException(nameof(products));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public string GenerateTicketContent(int bookingId)
    {
        var data = LoadData(bookingId);
        var culture = CultureInfo.GetCultureInfo("vi-VN");
        var builder = new StringBuilder();
        builder.AppendLine("================================================");
        builder.AppendLine("               CINEMA MANAGEMENT                ");
        builder.AppendLine("                VÉ / HÓA ĐƠN                    ");
        builder.AppendLine("================================================");
        builder.AppendLine($"Mã hóa đơn : {data.Booking.Code}");
        builder.AppendLine($"Ngày tạo   : {data.Booking.CreatedAt:dd/MM/yyyy HH:mm}");
        builder.AppendLine($"Phim       : {data.Showtime?.MovieTitle ?? "Không rõ"}");
        builder.AppendLine($"Phòng      : {data.Showtime?.RoomName ?? "Không rõ"}");
        if (data.Showtime is not null)
            builder.AppendLine($"Suất chiếu : {data.Showtime.StartTime:dd/MM/yyyy HH:mm}");
        builder.AppendLine("------------------------------------------------");
        builder.AppendLine("VÉ XEM PHIM");
        foreach (var ticket in data.Tickets)
        {
            var seatCode = data.SeatCodes.GetValueOrDefault(ticket.SeatId, $"#{ticket.SeatId}");
            builder.AppendLine($"Ghế {seatCode,-8} {ticket.Price.ToString("N0", culture)} đ");
        }

        if (data.Products.Count > 0)
        {
            builder.AppendLine("------------------------------------------------");
            builder.AppendLine("SẢN PHẨM");
            foreach (var item in data.Products)
            {
                var name = _products.GetById(item.ProductId)?.Name ?? $"Sản phẩm #{item.ProductId}";
                builder.AppendLine($"{name} × {item.Quantity}  {(item.Price * item.Quantity).ToString("N0", culture)} đ");
            }
        }

        builder.AppendLine("------------------------------------------------");
        builder.AppendLine($"Tạm tính   : {data.Booking.Subtotal.ToString("N0", culture)} đ");
        builder.AppendLine($"Giảm giá   : {data.Booking.Discount.ToString("N0", culture)} đ");
        builder.AppendLine($"Thanh toán : {data.Booking.PaymentMethod}");
        builder.AppendLine($"Tổng tiền  : {data.Booking.Total.ToString("N0", culture)} đ");
        builder.AppendLine("================================================");
        builder.AppendLine("Cảm ơn quý khách. Chúc quý khách xem phim vui vẻ!");
        return builder.ToString();
    }

    public void ExportTicketToPdf(int bookingId, string filepath)
    {
        if (string.IsNullOrWhiteSpace(filepath))
            throw new ArgumentException("Đường dẫn lưu file không hợp lệ.", nameof(filepath));

        var data = LoadData(bookingId);
        var culture = CultureInfo.GetCultureInfo("vi-VN");
        var directory = Path.GetDirectoryName(Path.GetFullPath(filepath));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(28);
                page.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(10));
                page.Content().Column(column =>
                {
                    column.Spacing(7);
                    column.Item().AlignCenter().Text("CINEMA MANAGEMENT").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                    column.Item().AlignCenter().Text("VÉ XEM PHIM / HÓA ĐƠN THANH TOÁN").FontSize(11).SemiBold();
                    column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    column.Item().Text($"Mã hóa đơn: {data.Booking.Code}").Bold();
                    column.Item().Text($"Ngày tạo: {data.Booking.CreatedAt:dd/MM/yyyy HH:mm}");
                    column.Item().Text($"Phim: {data.Showtime?.MovieTitle ?? "Không rõ"}");
                    column.Item().Text($"Phòng: {data.Showtime?.RoomName ?? "Không rõ"}");
                    if (data.Showtime is not null)
                        column.Item().Text($"Suất chiếu: {data.Showtime.StartTime:dd/MM/yyyy HH:mm}");
                    column.Item().PaddingTop(7).Text("VÉ XEM PHIM").Bold();
                    foreach (var ticket in data.Tickets)
                    {
                        var seatCode = data.SeatCodes.GetValueOrDefault(ticket.SeatId, $"#{ticket.SeatId}");
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Vé #{ticket.Id} · Ghế {seatCode}");
                            row.ConstantItem(110).AlignRight().Text($"{ticket.Price.ToString("N0", culture)} đ");
                        });
                    }

                    if (data.Products.Count > 0)
                    {
                        column.Item().PaddingTop(7).Text("SẢN PHẨM").Bold();
                        foreach (var item in data.Products)
                        {
                            var name = _products.GetById(item.ProductId)?.Name ?? $"Sản phẩm #{item.ProductId}";
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"{name} × {item.Quantity}");
                                row.ConstantItem(110).AlignRight().Text($"{(item.Price * item.Quantity).ToString("N0", culture)} đ");
                            });
                        }
                    }

                    column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Tạm tính");
                        row.ConstantItem(110).AlignRight().Text($"{data.Booking.Subtotal.ToString("N0", culture)} đ");
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Giảm giá");
                        row.ConstantItem(110).AlignRight().Text($"{data.Booking.Discount.ToString("N0", culture)} đ");
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Phương thức thanh toán");
                        row.ConstantItem(110).AlignRight().Text(data.Booking.PaymentMethod.ToString());
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TỔNG THANH TOÁN").Bold().FontSize(13);
                        row.ConstantItem(140).AlignRight().Text($"{data.Booking.Total.ToString("N0", culture)} đ").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                    });
                    column.Item().PaddingTop(16).AlignCenter().Text("Cảm ơn quý khách. Chúc quý khách xem phim vui vẻ!").Italic().FontColor(Colors.Grey.Darken2);
                });
            });
        }).GeneratePdf(filepath);
    }


    private TicketDocumentData LoadData(int bookingId)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        if (bookingId <= 0)
            throw new BusinessException("Mã hóa đơn không hợp lệ.");

        var booking = _bookings.GetById(bookingId)
                      ?? throw new BusinessException(
                          $"Không tìm thấy hóa đơn #{bookingId}.");

        if (booking.Status != BookingStatus.Paid)
        {
            throw new BusinessException(
                "Chỉ có thể in hoặc xuất vé cho hóa đơn đã thanh toán.");
        }

        var tickets = _tickets.GetByBookingId(bookingId);

        if (tickets.Count == 0)
            throw new BusinessException("Hóa đơn chưa có vé nào.");

        var showtimeIds = tickets
            .Select(ticket => ticket.ShowtimeId)
            .Distinct()
            .ToList();

        if (showtimeIds.Count != 1)
        {
            throw new BusinessException(
                "Dữ liệu hóa đơn không hợp lệ: các vé không thuộc cùng một suất chiếu.");
        }

        var showtime = _showtimes.GetById(showtimeIds[0])
                       ?? throw new BusinessException(
                           "Không tìm thấy thông tin suất chiếu của hóa đơn.");

        var seats = _seats.GetByRoom(showtime.RoomId).ToList();

        var seatCodes = seats.ToDictionary(
            seat => seat.Id,
            seat => seat.SeatCode);

        if (tickets.Any(ticket => !seatCodes.ContainsKey(ticket.SeatId)))
        {
            throw new BusinessException(
                "Một hoặc nhiều ghế trong hóa đơn không còn tồn tại trong sơ đồ phòng.");
        }

        var products = _bookings.GetProductsByBookingId(bookingId);

        return new TicketDocumentData(
            booking,
            tickets,
            products,
            showtime,
            seatCodes);
    }


    private sealed record TicketDocumentData(
        Booking Booking,
        List<Ticket> Tickets,
        List<BookingProduct> Products,
        Showtime? Showtime,
        Dictionary<int, string> SeatCodes);
}
