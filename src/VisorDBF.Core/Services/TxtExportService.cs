using System.Globalization;
using System.Text;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Models;
namespace VisorDBF.Core.Services;

public class TxtExportService : IExportService
{
    public async Task ExportAsync(
        DbfFile file,
        ExportConfiguration config,
        string outputPath,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        try
        {
            using var writer = new StreamWriter(
                outputPath,
                append: false,
                encoding: config.OutputEncoding,
                bufferSize: 65536);

            int targetCount = config.RowLimitMode == RowLimitMode.FirstN
                ? Math.Min(config.MaxRows, file.RecordCount)
                : file.RecordCount;

            int throttleInterval = Math.Max(1, targetCount / 1000);

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

                var line = BuildLine(record, file.Fields, config);
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
        ExportConfiguration config)
    {
        var values = fields.Select(f => FormatValue(record.Values.GetValueOrDefault(f.Name)));
        var line = string.Join(config.ColumnSeparator, values);
        if (!string.IsNullOrEmpty(config.RowEndDelimiter))
            line += config.RowEndDelimiter;
        return line;
    }

    internal static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IFormattable fmt => fmt.ToString(null, CultureInfo.CurrentCulture),
        _ => value.ToString() ?? string.Empty
    };
}
