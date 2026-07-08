using System.Windows;
using VisorDBF.Core.Models;
using VisorDBF.UI.ViewModels;
namespace VisorDBF.UI.Views;

public partial class SybaseConnectionDialog : Window
{
    public SybaseConnectionDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SybaseConnectionViewModel vm)
        {
            PasswordBox.Password = vm.Password;
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SybaseConnectionViewModel.Password))
                    PasswordBox.Password = vm.Password;
            };
        }
    }

    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SybaseConnectionViewModel vm)
        {
            vm.SetPasswordFromDialog(PasswordBox.Password);
            vm.SaveCommand.Execute(null);
        }
        DialogResult = true;
    }
}