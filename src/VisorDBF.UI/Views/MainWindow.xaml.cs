using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using VisorDBF.Core.Models;
using VisorDBF.UI.ViewModels;

namespace VisorDBF.UI.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// Gestiona la generacion dinamica de columnas del DataGrid en respuesta a
/// cambios en MainViewModel.Fields.
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel? ViewModel => DataContext as MainViewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SubscribeToViewModel();
    }

    private void SubscribeToViewModel()
    {
        if (DataContext is MainViewModel vm)
            vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Fields) && ViewModel is { } vm)
            GenerateColumns(vm.Fields);
    }

    /// <summary>
    /// Regenera las columnas del DataGrid a partir de la lista de campos DBF.
    /// Cada columna tiene un header con nombre del campo (SemiBold) y tipo entre parentesis.
    /// El binding usa PropertyPath para manejar nombres de campo con caracteres especiales.
    /// </summary>
    private void GenerateColumns(IReadOnlyList<DbfField> fields)
    {
        MainDataGrid.Columns.Clear();

        foreach (var field in fields)
        {
            // Header: StackPanel con nombre en SemiBold y tipo en fuente pequeña
            var headerPanel = new StackPanel { Orientation = Orientation.Vertical };

            headerPanel.Children.Add(new TextBlock
            {
                Text = field.Name,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            headerPanel.Children.Add(new TextBlock
            {
                Text = $"({field.Type.ToDisplayString()})",
                FontSize = 11,
                FontWeight = FontWeights.Normal,
                Foreground = new SolidColorBrush(Color.FromRgb(0x5E, 0x5E, 0x5E))
            });

            // Binding al indexador del Dictionary con PropertyPath para soportar
            // nombres de campo con espacios o caracteres especiales
            var col = new DataGridTextColumn
            {
                Header = headerPanel,
                Binding = new Binding
                {
                    Path = new PropertyPath($"Values[{field.Name}]")
                },
                MinWidth = 60,
                MaxWidth = field.Type == DbfFieldType.Memo ? 120 : 300,
                Width = DataGridLength.Auto
            };

            MainDataGrid.Columns.Add(col);
        }
    }

    private void MenuItem_Salir_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
