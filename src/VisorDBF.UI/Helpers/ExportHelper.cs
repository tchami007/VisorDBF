using VisorDBF.Core.Models;
using VisorDBF.Core.Services;

namespace VisorDBF.UI.Helpers;

public static class ExportHelper
{
    public static Task RunExportAsync(
        IExportService exportService,
        DbfFile file,
        ExportConfiguration config,
        string outputPath,
        ColumnFormatConfiguration columnFormats,
        IProgress<int> progress,
        SynchronizationContext? syncContext,
        CancellationToken cancellationToken,
        Action onComplete,
        Action onCancelled)
    {
        return Task.Run(async () =>
        {
            try
            {
                await exportService.ExportAsync(
                    file, config, outputPath, progress, cancellationToken, columnFormats);

                syncContext?.Post(_ => onComplete(), null);
            }
            catch (OperationCanceledException)
            {
                syncContext?.Post(_ => onCancelled(), null);
                throw;
            }
        }, cancellationToken);
    }
}
