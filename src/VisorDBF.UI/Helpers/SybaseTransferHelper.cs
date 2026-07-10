using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Models;
using VisorDBF.Core.Services;

namespace VisorDBF.UI.Helpers;

public static class SybaseTransferHelper
{
    public static Task RunTransferAsync(
        ISybaseExportService sybaseExportService,
        DbfFile file,
        SybaseConnectionConfig config,
        IProgress<int> progress,
        SynchronizationContext? syncContext,
        CancellationToken cancellationToken,
        Action onComplete,
        Action onCancelled,
        Action<string> onProbeError,
        Action<string> onFatalError)
    {
        return Task.Run(async () =>
        {
            try
            {
                var probeResult = await sybaseExportService.ProbeFirstRecordAsync(
                    file, config, cancellationToken);

                if (!probeResult.Success)
                {
                    syncContext?.Post(_ => onProbeError(probeResult.ErrorMessage ?? "Error desconocido durante el probe."), null);
                    return;
                }

                await sybaseExportService.TransferAsync(
                    file, config, progress, cancellationToken);

                syncContext?.Post(_ => onComplete(), null);
            }
            catch (OperationCanceledException)
            {
                syncContext?.Post(_ => onCancelled(), null);
                throw;
            }
            catch (ExportException ex)
            {
                syncContext?.Post(_ => onFatalError(ex.Message), null);
            }
            catch (Exception ex)
            {
                syncContext?.Post(_ =>
                    onFatalError($"Se produjo un error durante el traspaso a Sybase.\n\n{ex.Message}"), null);
            }
        }, cancellationToken);
    }
}
