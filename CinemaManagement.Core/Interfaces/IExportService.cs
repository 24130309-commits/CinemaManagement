using System.Collections.Generic;

namespace CinemaManagement.Core.Interfaces;

public interface IExportService
{
    void ExportToExcel<T>(IEnumerable<T> data, string path);
    void ExportToPdf<T>(IEnumerable<T> data, string path);
}