using System.Windows;
using VisorDBF.Core.Models;
using VisorDBF.UI.ViewModels;
namespace VisorDBF.UI.Views;

public partial class SybaseConnectionDialog : Window
{
    private bool _isSyncing;

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
            PasswordBox.PasswordChanged += (_, _) =>
            {
                if (_isSyncing) return;
                _isSyncing = true;
                vm.SetPasswordFromDialog(PasswordBox.Password);
                _isSyncing = false;
            };
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != nameof(SybaseConnectionViewModel.Password))
                    return;
                if (_isSyncing) return;
                _isSyncing = true;
                PasswordBox.Password = vm.Password;
                _isSyncing = false;
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