using VisorDBF.Core.Models;

namespace VisorDBF.Core.Services;

public sealed record ProbeResult(bool Success, string? ErrorMessage);

public interface ISybaseExportService
{
    Task TransferAsync(
        DbfFile file,
        SybaseConnectionConfig config,
        IProgress<int> progress,
        CancellationToken cancellationToken);

    Task<ProbeResult> ProbeFirstRecordAsync(
        DbfFile file,
        SybaseConnectionConfig config,
        CancellationToken cancellationToken);
}