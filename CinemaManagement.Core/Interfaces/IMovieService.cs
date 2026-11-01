using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces
{
    public interface IMovieService
    {
        IEnumerable<Movie> GetAllMovies();
        Movie GetMovieById(int id);
        void AddMovie(Movie movie);
        void UpdateMovie(Movie movie);
        void DeleteMovie(int id);
    }
}