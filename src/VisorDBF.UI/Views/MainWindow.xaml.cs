using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using VisorDBF.Core.Models;
using VisorDBF.UI.Converters;
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
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            vm.ColumnFormatsChanged += OnColumnFormatsChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Fields) && ViewModel is { } vm)
            GenerateColumns(vm.Fields);
    }

    private void OnColumnFormatsChanged(object? sender, EventArgs e)
    {
        if (ViewModel is { } vm)
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

        var vm = ViewModel;
        bool applyFormats = vm?.AreFormatsActive == true;
        var formats = vm?.CurrentColumnFormats.Formats;

        foreach (var field in fields)
        {
            string? formatString = null;
            bool hasFormat = applyFormats && formats != null
                && formats.TryGetValue(field.Name, out formatString)
                && !string.IsNullOrEmpty(formatString);

            var headerPanel = new StackPanel { Orientation = Orientation.Vertical };

            var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
            nameRow.Children.Add(new TextBlock
            {
                Text = field.Name,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            if (hasFormat)
            {
                nameRow.Children.Add(new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Color.FromRgb(0x00, 0x90, 0x50)),
                    Margin = new Thickness(4, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = $"Formato: {formatString}"
                });
            }

            headerPanel.Children.Add(nameRow);

            headerPanel.Children.Add(new TextBlock
            {
                Text = $"({field.Type.ToDisplayString()})",
                FontSize = 11,
                FontWeight = FontWeights.Normal,
                Foreground = new SolidColorBrush(Color.FromRgb(0x5E, 0x5E, 0x5E))
            });

            var binding = new Binding
            {
                Path = new PropertyPath($"Values[{field.Name}]")
            };

            if (hasFormat)
            {
                binding.Converter = new ColumnFormatConverter();
                binding.ConverterParameter = formatString;
            }

            var col = new DataGridTextColumn
            {
                Header = headerPanel,
                Binding = binding,
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
