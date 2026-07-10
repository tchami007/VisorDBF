using System.Globalization;
using System.Text;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Models;
namespace VisorDBF.Core.Services;

public class TxtExportService : IExportService
{
    private readonly IColumnFormatService? _formatService;

    public TxtExportService(IColumnFormatService? formatService = null)
    {
        _formatService = formatService;
    }

    public async Task ExportAsync(
        DbfFile file,
        ExportConfiguration config,
        string outputPath,
        IProgress<int> progress,
        CancellationToken cancellationToken,
        ColumnFormatConfiguration? columnFormats = null)
    {
        try
        {
            var encoding = config.OutputEncoding.CodePage == 65001
                ? ExportConfiguration.UTF8NoBOM
                : config.OutputEncoding;

            using var writer = new StreamWriter(
                outputPath,
                append: false,
                encoding: encoding,
                bufferSize: 65536);

            int targetCount = config.RowLimitMode == RowLimitMode.FirstN
                ? Math.Min(config.MaxRows, file.RecordCount)
                : file.RecordCount;

            int throttleInterval = Math.Max(1, targetCount / 1000);

            var numberCulture = GetNumberCulture(config.DecimalSeparator);

            if (config.IncludeHeader)
            {
                var header = string.Join(config.ColumnSeparator, file.Fields.Select(f => f.Name));
                await writer.WriteLineAsync(header.AsMemory(), cancellationToken);
            }

            int exported = 0;
            foreach (var record in file.Records)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (config.RowLimitMode == RowLimitMode.FirstN && exported >= config.MaxRows)
                    break;

                var line = BuildLine(record, file.Fields, config, columnFormats, _formatService, numberCulture);
                await writer.WriteLineAsync(line.AsMemory(), cancellationToken);

                exported++;

                if (exported % throttleInterval == 0 || exported == targetCount)
                    progress.Report(exported);
            }
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            throw new ExportException(
                "No se pudo escribir el archivo de salida. Verifique los permisos y el espacio disponible en disco.",
                outputPath, ex);
        }
    }

    internal static string BuildLine(
        DbfRecord record,
        IReadOnlyList<DbfField> fields,
        ExportConfiguration config,
        ColumnFormatConfiguration? columnFormats = null,
        IColumnFormatService? formatService = null,
        CultureInfo? numberCulture = null)
    {
        numberCulture ??= GetNumberCulture(config.DecimalSeparator);
        var values = fields.Select(f => FormatValue(
            record.Values.GetValueOrDefault(f.Name),
            f.Name,
            columnFormats,
            formatService,
            numberCulture));
        var line = string.Join(config.ColumnSeparator, values);
        if (!string.IsNullOrEmpty(config.RowEndDelimiter))
            line += config.RowEndDelimiter;
        return line;
    }

    internal static string FormatValue(
        object? value,
        string? fieldName = null,
        ColumnFormatConfiguration? columnFormats = null,
        IColumnFormatService? formatService = null,
        CultureInfo? numberCulture = null)
    {
        numberCulture ??= CultureInfo.InvariantCulture;

        if (columnFormats?.IsActive == true && fieldName != null
            && columnFormats.Formats.TryGetValue(fieldName, out var format)
            && !string.IsNullOrEmpty(format)
            && value is IFormattable formattable)
        {
            try
            {
                return formattable.ToString(format, numberCulture);
            }
            catch (FormatException)
            {
                return value.ToString() ?? string.Empty;
            }
        }

        return value switch
        {
            null => string.Empty,
            DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IFormattable fmt => fmt.ToString(null, numberCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    internal static string FormatValue(object? value)
        => FormatValue(value, null, null, null, null);

    internal static CultureInfo GetNumberCulture(string decimalSeparator)
    {
        if (decimalSeparator == ",")
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ",";
            culture.NumberFormat.NumberGroupSeparator = "";
            return culture;
        }
        return CultureInfo.InvariantCulture;
    }
}
