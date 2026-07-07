using System.Text;
using FluentAssertions;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Models;
using VisorDBF.Core.Services;
using Xunit;

namespace VisorDBF.Core.Tests.Services;

public class TxtExportServiceTests
{
    static TxtExportServiceTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static DbfFile CreateSampleFile(int recordCount = 3)
    {
        var fields = new List<DbfField>
        {
            new("NOMBRE", DbfFieldType.Character, 20, 0, 0),
            new("EDAD", DbfFieldType.Numeric, 3, 0, 1),
            new("FECHA", DbfFieldType.Date, 8, 0, 2)
        };

        var records = new List<DbfRecord>();
        for (int i = 0; i < recordCount; i++)
        {
            records.Add(new DbfRecord(i, false, new Dictionary<string, object?>
            {
                ["NOMBRE"] = $"Persona {i + 1}",
                ["EDAD"] = 25 + i,
                ["FECHA"] = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i)
            }));
        }

        return new DbfFile
        {
            Fields = fields,
            Records = records,
            RecordCount = recordCount
        };
    }

    [Fact]
    public void FormatValue_Null_ReturnsEmpty()
    {
        TxtExportService.FormatValue(null).Should().Be(string.Empty);
    }

    [Fact]
    public void FormatValue_DateTime_ReturnsYyyyMmDd()
    {
        var dt = new DateTime(2024, 7, 15);
        TxtExportService.FormatValue(dt).Should().Be("2024-07-15");
    }

    [Fact]
    public void FormatValue_String_ReturnsSame()
    {
        TxtExportService.FormatValue("hello").Should().Be("hello");
    }

    [Fact]
    public void FormatValue_Int_ReturnsToString()
    {
        TxtExportService.FormatValue(42).Should().Be("42");
    }

    [Fact]
    public void BuildLine_WithSemicolonSeparator_JoinsCorrectly()
    {
        var file = CreateSampleFile(1);
        var record = file.Records[0];
        var config = ExportConfiguration.Default;

        var line = TxtExportService.BuildLine(record, file.Fields, config);

        line.Should().Be("Persona 1;25;2024-01-01");
    }

    [Fact]
    public void BuildLine_WithRowEndDelimiter_AppendsIt()
    {
        var file = CreateSampleFile(1);
        var record = file.Records[0];
        var config = new ExportConfiguration { RowEndDelimiter = "|END" };

        var line = TxtExportService.BuildLine(record, file.Fields, config);

        line.Should().EndWith("|END");
    }

    [Fact]
    public async Task ExportAsync_WithDefaultConfig_WritesCorrectFile()
    {
        var file = CreateSampleFile(2);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid()}.txt");
        var progressReports = new List<int>();

        try
        {
            var progress = new Progress<int>(v => progressReports.Add(v));
            var service = new TxtExportService();

            await service.ExportAsync(file, ExportConfiguration.Default, outputPath, progress, CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath);
            lines.Should().HaveCount(3);
            lines[0].Should().Be("NOMBRE;EDAD;FECHA");
                lines[1].Should().Be("Persona 1;25;2024-01-01");
                lines[2].Should().Be("Persona 2;26;2024-01-02");

            progressReports.Should().Contain(2);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_WithoutHeader_WritesNoHeader()
    {
        var file = CreateSampleFile(1);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid()}.txt");
        var config = new ExportConfiguration { IncludeHeader = false };

        try
        {
            var service = new TxtExportService();
            await service.ExportAsync(file, config, outputPath, new Progress<int>(), CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath);
            lines.Should().HaveCount(1);
            lines[0].Should().Be("Persona 1;25;2024-01-01");
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_FirstN_LimitsRows()
    {
        var file = CreateSampleFile(10);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid()}.txt");
        var config = new ExportConfiguration
        {
            RowLimitMode = RowLimitMode.FirstN,
            MaxRows = 3
        };

        try
        {
            var service = new TxtExportService();
            await service.ExportAsync(file, config, outputPath, new Progress<int>(), CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath);
            lines.Should().HaveCount(4);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_Cancellation_DeletesPartialFile()
    {
        var file = CreateSampleFile(100);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid()}.txt");
        var cts = new CancellationTokenSource();

        try
        {
            var service = new TxtExportService();
            var progress = new Progress<int>(v =>
            {
                if (v >= 5) cts.Cancel();
            });

            Func<Task> act = () => service.ExportAsync(file, ExportConfiguration.Default, outputPath, progress, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();

            File.Exists(outputPath).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_InvalidPath_ThrowsExportException()
    {
        var file = CreateSampleFile(1);
        var invalidPath = @"Z:\invalid\path\output.txt";

        var service = new TxtExportService();
        Func<Task> act = () => service.ExportAsync(
            file, ExportConfiguration.Default, invalidPath, new Progress<int>(), CancellationToken.None);
        await act.Should().ThrowAsync<ExportException>();
    }

    [Fact]
    public async Task ExportAsync_PipeSeparator_UsesPipe()
    {
        var file = CreateSampleFile(1);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid()}.txt");
        var config = new ExportConfiguration { ColumnSeparator = "|" };

        try
        {
            var service = new TxtExportService();
            await service.ExportAsync(file, config, outputPath, new Progress<int>(), CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(outputPath);
            lines[1].Should().Match("*|*|*");
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_Windows1252Encoding_WritesCorrectEncoding()
    {
        var file = CreateSampleFile(1);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid()}.txt");
        var config = new ExportConfiguration
        {
            OutputEncoding = Encoding.GetEncoding("windows-1252")
        };

        try
        {
            var service = new TxtExportService();
            await service.ExportAsync(file, config, outputPath, new Progress<int>(), CancellationToken.None);

            using var reader = new StreamReader(outputPath, Encoding.GetEncoding("windows-1252"));
            var content = await reader.ReadToEndAsync();
            content.Should().Contain("Persona 1");
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}
