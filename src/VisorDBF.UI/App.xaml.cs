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
        var txtExportService = new TxtExportService();
        var sqlExportService = new SqlExportService();
        var columnFormatService = new ColumnFormatService();
        var sybaseExportService = new SybaseExportService();

        var mainViewModel = new MainViewModel(dbfReader, encodingDetector, txtExportService, sqlExportService, sybaseExportService, columnFormatService);

        var mainWindow = new MainWindow();
        mainWindow.DataContext = mainViewModel;
        mainWindow.Show();
    }
}
