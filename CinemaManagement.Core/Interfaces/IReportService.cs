using System;
using System.Collections.Generic;
using CinemaManagement.Core.DTOs;

namespace CinemaManagement.Core.Interfaces;

public interface IReportService
{
    IEnumerable<RevenueByMovieDto> GetRevenueByMovie(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<OccupancyDto> GetOccupancyReport(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<dynamic> GetRevenueByDay(
        DateTime from,
        DateTime to);

    dynamic GetDashboardStats();
}