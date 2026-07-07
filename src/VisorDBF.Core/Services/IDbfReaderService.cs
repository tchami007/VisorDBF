using System.Text;
using VisorDBF.Core.Models;
namespace VisorDBF.Core.Services;

public interface IDbfReaderService
{
    Task<DbfFile> ReadAsync(string filePath, Encoding encoding, CancellationToken cancellationToken = default);
}
