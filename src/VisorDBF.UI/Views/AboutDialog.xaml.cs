using System.Reflection;
using System.Windows;

namespace VisorDBF.UI.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var versionStr = version != null
            ? $"Version {version.Major}.{version.Minor}.{version.Build}"
            : "Version 1.0.0";
        VersionText.Text = versionStr;
        CopyrightText.Text = $"{DateTime.Now.Year} VisorDBF";
    }
}
