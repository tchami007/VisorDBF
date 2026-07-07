using System.ComponentModel;
using System.Windows;
using VisorDBF.UI.ViewModels;
namespace VisorDBF.UI.Views;

public partial class ExportProgressDialog : Window
{
    private ExportProgressDialogViewModel? ViewModel => DataContext as ExportProgressDialogViewModel;

    public ExportProgressDialog()
    {
        InitializeComponent();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (ViewModel is { IsExporting: true })
            e.Cancel = true;
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = ViewModel?.IsComplete == true;
    }
}
