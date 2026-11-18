namespace CinemaManagement.Core.Enums;

public enum PaymentMethod
{
    Cash = 0,
    Card = 1,
    EWallet = 2,

    // Tương thích tên phương thức cũ.
    CreditCard = Card,
    MOMO = EWallet,
    VNPay = EWallet
}