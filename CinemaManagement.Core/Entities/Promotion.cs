namespace CinemaManagement.Core.Entities;

public class Promotion
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Type { get; set; } = "Percent";

    public decimal Value { get; set; }

    public decimal DiscountPercent
    {
        get => string.Equals(
            Type,
            "Percent",
            StringComparison.OrdinalIgnoreCase)
            ? Value
            : 0m;

        set
        {
            Type = "Percent";
            Value = value;
        }
    }

    public decimal MaxDiscountAmount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal MinOrder { get; set; }

    public int UsageLimit { get; set; }

    public int UsedCount { get; set; }

    // Trạng thái do quản trị viên thiết lập.
    public bool IsActive { get; set; } = true;

    // Trạng thái hiệu lực tại thời điểm hiện tại.
    public bool IsCurrentlyValid =>
        IsActive &&
        DateTime.Now >= StartDate &&
        DateTime.Now <= EndDate &&
        (UsageLimit <= 0 || UsedCount < UsageLimit);
}