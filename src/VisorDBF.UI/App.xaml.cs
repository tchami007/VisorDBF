using System.Text;
using System.Windows;
using VisorDBF.Core.Services;
using VisorDBF.UI.ViewModels;
using VisorDBF.UI.Views;

namespace VisorDBF.UI;

/// <summary>
/// Punto de entrada de la aplicacion.
/// Registra encoding providers y construye el grafo de dependencias manualmente.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // CRITICO: registrar antes de cualquier operacion de encoding
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        base.OnStartup(e);

        var encodingDetector = new EncodingDetectionService();
        var dbfReader = new DbfReaderService();
        var mainViewModel = new MainViewModel(dbfReader, encodingDetector);

        var mainWindow = new MainWindow();
        mainWindow.DataContext = mainViewModel;
        mainWindow.Show();
    }
}
