using System.Windows;
namespace VisorDBF.UI.Views;

public partial class ColumnFormatsWindow : Window
{
    public ColumnFormatsWindow()
    {
        InitializeComponent();
    }

    private void BtnAceptar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ColumnFormatsViewModel vm)
            vm.Apply();
        DialogResult = true;
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
