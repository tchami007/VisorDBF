using System.Text;
using VisorDBF.Core.Models;
namespace VisorDBF.Core.Services;

public interface IExportService
{
    Task ExportAsync(
        DbfFile file,
        ExportConfiguration config,
        string outputPath,
        IProgress<int> progress,
        CancellationToken cancellationToken);
}
