using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Entities;

public class Movie
{
    private int _durationMinutes;

    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int DurationMinutes
    {
        get => _durationMinutes;
        set => _durationMinutes = value;
    }

    public int Duration
    {
        get => _durationMinutes;
        set => _durationMinutes = value;
    }

    public string PosterPath { get; set; } = string.Empty;

    public string TrailerUrl { get; set; } = string.Empty;

    public int AgeLimit { get; set; }

    public DateTime ReleaseDate { get; set; }

    public MovieStatus Status { get; set; }
}