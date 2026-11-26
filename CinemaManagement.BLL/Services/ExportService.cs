using System.Reflection;
using System.Text;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public sealed class ExportService : IExportService
{
    public ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void ExportToExcel<T>(IEnumerable<T> data, string path)
    {
        var rows = data?.ToList()
            ?? throw new BusinessException("Không có dữ liệu để xuất.");

        if (string.IsNullOrWhiteSpace(path))
            throw new BusinessException("Đường dẫn xuất tệp không hợp lệ.");

        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (properties.Length == 0 && rows.Count > 0)
            properties = rows[0]!.GetType().GetProperties();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("BaoCao");

        for (var col = 0; col < properties.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = properties[col].Name;
            sheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        for (var row = 0; row < rows.Count; row++)
        {
            for (var col = 0; col < properties.Length; col++)
            {
                var cell = sheet.Cell(row + 2, col + 1);
                var value = properties[col].GetValue(rows[row]);

                switch (value)
                {
                    case null:
                        cell.Value = string.Empty;
                        break;

                    case DateTime date:
                        cell.Value = date;
                        break;

                    case decimal decimalValue:
                        cell.Value = (double)decimalValue;
                        break;

                    case int intValue:
                        cell.Value = intValue;
                        break;

                    case long longValue:
                        cell.Value = longValue;
                        break;

                    case double doubleValue:
                        cell.Value = doubleValue;
                        break;

                    case float floatValue:
                        cell.Value = (double)floatValue;
                        break;

                    case bool flag:
                        cell.Value = flag;
                        break;

                    case Enum enumeration:
                        cell.Value = enumeration.ToString();
                        break;

                    default:
                        cell.Value = Convert.ToString(value) ?? string.Empty;
                        break;
                }
            }
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        workbook.SaveAs(path);
    }

    public void ExportToPdf<T>(IEnumerable<T> data, string path)
    {
        var rows = data?.ToList()
            ?? throw new BusinessException("Không có dữ liệu để xuất.");

        if (string.IsNullOrWhiteSpace(path))
            throw new BusinessException("Đường dẫn xuất tệp không hợp lệ.");

        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (properties.Length == 0 && rows.Count > 0)
            properties = rows[0]!.GetType().GetProperties();

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(24);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header()
                .Text("CINEMA MANAGEMENT – BÁO CÁO")
                .SemiBold()
                .FontSize(16);

            page.Content()
                .PaddingVertical(12)
                .Column(column =>
                {
                    column.Item()
                        .Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontColor(Colors.Grey.Darken1);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            foreach (var _ in properties)
                                cols.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var property in properties)
                            {
                                header.Cell()
                                    .Background(Colors.Grey.Lighten2)
                                    .Padding(4)
                                    .Text(property.Name)
                                    .SemiBold();
                            }
                        });

                        foreach (var row in rows)
                        {
                            foreach (var property in properties)
                            {
                                table.Cell()
                                    .BorderBottom(0.5f)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(4)
                                    .Text(
                                        Convert.ToString(
                                            property.GetValue(row))
                                        ?? string.Empty);
                            }
                        }
                    });
                });

            page.Footer()
                .AlignCenter()
                .Text(text =>
                {
                    text.Span("Cinema Management | Trang ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
        })).GeneratePdf(path);
    }

    public void ExportToCsv<T>(IEnumerable<T> data, string path)
    {
        var rows = data?.ToList()
            ?? throw new BusinessException("Không có dữ liệu để xuất.");

        if (string.IsNullOrWhiteSpace(path))
            throw new BusinessException("Đường dẫn xuất tệp không hợp lệ.");

        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (properties.Length == 0 && rows.Count > 0)
            properties = rows[0]!.GetType().GetProperties();

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder();

        builder.AppendLine(
            string.Join(
                ",",
                properties.Select(property =>
                    EscapeCsv(property.Name, protectFormula: true))));

        foreach (var row in rows)
        {
            builder.AppendLine(
                string.Join(
                    ",",
                    properties.Select(property =>
                    {
                        var cell = property.GetValue(row);
                        return EscapeCsv(
                            Convert.ToString(cell) ?? string.Empty,
                            protectFormula: cell is string);
                    })));
        }

        File.WriteAllText(
            path,
            builder.ToString(),
            new UTF8Encoding(true));
    }

    private static string EscapeCsv(string value, bool protectFormula = false)
    {
        if (protectFormula)
        {
            var firstNonWhitespace = value.AsSpan().TrimStart();
            if (!firstNonWhitespace.IsEmpty &&
                firstNonWhitespace[0] is '=' or '+' or '-' or '@')
            {
                value = "'" + value;
            }
        }

        if (value.Contains('"'))
            value = value.Replace("\"", "\"\"");

        if (value.Contains(',') ||
            value.Contains('"') ||
            value.Contains('\r') ||
            value.Contains('\n'))
        {
            return $"\"{value}\"";
        }

        return value;
    }
}
