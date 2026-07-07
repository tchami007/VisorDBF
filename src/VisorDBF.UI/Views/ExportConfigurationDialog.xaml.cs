using System.Windows;
namespace VisorDBF.UI.Views;

public partial class ExportConfigurationDialog : Window
{
    public ExportConfigurationDialog()
    {
        InitializeComponent();
    }

    private void BtnAceptar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ExportConfigurationViewModel vm)
            vm.Apply();
        DialogResult = true;
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
