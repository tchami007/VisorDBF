using System.Windows;
namespace VisorDBF.UI.Views;

public partial class SaveProfileDialog : Window
{
    public SaveProfileDialog()
    {
        InitializeComponent();
    }

    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
