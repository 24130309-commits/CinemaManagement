using System.Collections.Generic;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Interfaces
{
    public interface IMovieRepository : IRepository<Movie>
    {
        IEnumerable<Movie> GetMoviesByStatus(MovieStatus status);
        bool IsTitleExists(string title, int? excludeId = null);
    }
}