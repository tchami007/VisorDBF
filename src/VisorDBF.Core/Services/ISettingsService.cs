using VisorDBF.Core.Models;
namespace VisorDBF.Core.Services;

public interface ISettingsService
{
    ApplicationSettings Load();
    void Save(ApplicationSettings settings);
    string GetFilePath();
    string GetDirectory();
    void CreateBackup(string reason);
}
