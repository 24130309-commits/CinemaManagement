using System;
using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Entities
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public DateTime ReleaseDate { get; set; }
        public MovieStatus Status { get; set; }
    }
}