using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.BLL.Session;
using System.Diagnostics;

namespace CinemaManagement.BLL.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _payments;
    private readonly CurrentUserSession _session;
    private readonly IAuditService? _audit;

    public PaymentService(
        IPaymentRepository payments,
        CurrentUserSession session,
        IAuditService? audit = null)
    {
        _payments = payments
                    ?? throw new ArgumentNullException(nameof(payments));
        _session = session
                   ?? throw new ArgumentNullException(nameof(session));
        _audit = audit;
    }

    public List<Payment> GetByBookingId(int bookingId)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        if (bookingId <= 0)
        {
            throw new BusinessException(
                "Mã hóa đơn không hợp lệ.");
        }

        return _payments.GetByBookingId(bookingId);
    }

    public Payment? GetByTransactionCode(string transactionCode)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        if (string.IsNullOrWhiteSpace(transactionCode))
            return null;

        return _payments.GetByTransactionCode(
            transactionCode.Trim());
    }

    public bool RefundBooking(int bookingId, string note)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "hoàn tiền");
        if (bookingId <= 0)
        {
            throw new BusinessException(
                "Mã hóa đơn không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(note))
        {
            throw new BusinessException(
                "Vui lòng nhập lý do hoàn tiền.");
        }

        if (note.Trim().Length > 500)
        {
            throw new BusinessException(
                "Lý do hoàn tiền không được vượt quá 500 ký tự.");
        }

        var result = _payments.RefundBooking(
            bookingId,
            note.Trim());

        if (!result)
        {
            throw new BusinessException(
                "Không thể hoàn tiền. Hóa đơn không tồn tại, chưa thanh toán hoặc đã được hoàn tiền.");
        }

        try
        {
            _audit?.Log(
                "Refund booking",
                "Bookings",
                bookingId,
                note.Trim());
        }
        catch (Exception ex)
        {
            Trace.TraceError(
                "Refund {0} was recorded, but its audit entry failed: {1}",
                bookingId,
                ex);
        }

        return true;
    }
}
