using VisorDBF.Core.Models;

namespace VisorDBF.Core.Services;

public interface ISybaseExportService
{
    Task TransferAsync(
        DbfFile file,
        SybaseConnectionConfig config,
        IProgress<int> progress,
        CancellationToken cancellationToken);

    Task<bool> ProbeFirstRecordAsync(
        DbfFile file,
        SybaseConnectionConfig config,
        CancellationToken cancellationToken);
}