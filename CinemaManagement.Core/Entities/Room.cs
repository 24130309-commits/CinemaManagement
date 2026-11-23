using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Entities;

public class Room
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int TotalRows { get; set; }

    public int TotalCols { get; set; }

    public ScreenType ScreenType { get; set; }

    public bool IsActive { get; set; } = true;

    public int SeatsPerRow
    {
        get => TotalCols;
        set => TotalCols = value;
    }

    public string RoomType
    {
        get => ScreenType switch
        {
            ScreenType.TwoD => "2D",
            ScreenType.ThreeD => "3D",
            ScreenType.IMAX => "IMAX",
            _ => "2D"
        };
        set
        {
            ScreenType = value?.Trim().ToUpperInvariant() switch
            {
                "3D" => ScreenType.ThreeD,
                "IMAX" => ScreenType.IMAX,
                _ => ScreenType.TwoD
            };
        }
    }
}