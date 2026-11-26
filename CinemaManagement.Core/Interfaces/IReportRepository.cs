using System;
using System.Collections.Generic;
using CinemaManagement.Core.DTOs;

namespace CinemaManagement.Core.Interfaces;

public partial interface IReportRepository
{
    IEnumerable<RevenueByMovieDto> RevenueByMovie(DateTime from, DateTime to);
    IEnumerable<OccupancyDto> OccupancyByShowtime(DateTime from, DateTime to);
    IEnumerable<RevenueByDayDto> RevenueByDay(DateTime from, DateTime to);
    IEnumerable<RevenueByMovieDto> TopMovies(int n);
}
