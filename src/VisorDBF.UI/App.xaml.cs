using System.Text;
using System.Windows;
using VisorDBF.Core.Services;
using VisorDBF.UI.ViewModels;
using VisorDBF.UI.Views;

namespace VisorDBF.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        base.OnStartup(e);

        var encodingDetector = new EncodingDetectionService();
        var dbfReader = new DbfReaderService();
        var exportService = new TxtExportService();

        var mainViewModel = new MainViewModel(dbfReader, encodingDetector, exportService);

        var mainWindow = new MainWindow();
        mainWindow.DataContext = mainViewModel;
        mainWindow.Show();
    }
}
