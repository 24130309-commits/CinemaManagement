using CinemaManagement.BLL.Services;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public sealed class ExportServiceTests
{
    [Test]
    public void ExportToCsv_ProtectsFormulaText_AndPreservesNumericValues()
    {
        var directory = Path.Combine(Path.GetTempPath(), "CinemaManagementTests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "export.csv");

        try
        {
            new ExportService().ExportToCsv(
                new[] { new ExportRow(" =HYPERLINK(\"https://example.invalid\")", -125m) },
                path);

            var csv = File.ReadAllText(path, System.Text.Encoding.UTF8);
            Assert.That(csv, Does.Contain("' =HYPERLINK"));
            Assert.That(csv, Does.Contain(",-125"));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private sealed record ExportRow(string Description, decimal Amount);
}
