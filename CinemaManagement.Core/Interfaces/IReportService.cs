using System;
using System.Collections.Generic;
using CinemaManagement.Core.DTOs;

namespace CinemaManagement.Core.Interfaces;

public interface IReportService
{
    IEnumerable<RevenueByMovieDto> GetRevenueByMovie(DateTime from, DateTime to);
    IEnumerable<OccupancyDto> GetOccupancy(DateTime from, DateTime to);
    IEnumerable<dynamic> GetRevenueByDay(DateTime from, DateTime to);
    dynamic GetDashboardStats();
}