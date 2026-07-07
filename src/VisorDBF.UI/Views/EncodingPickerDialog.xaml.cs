using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using VisorDBF.Core.Models;
using VisorDBF.UI.ViewModels;

namespace VisorDBF.UI.Views;

/// <summary>
/// Dialogo modal para seleccionar la codificacion de lectura de un archivo DBF.
/// Muestra una lista de encodings disponibles y una vista previa de los primeros
/// 5 registros con la codificacion seleccionada.
/// </summary>
public partial class EncodingPickerDialog : Window
{
    private EncodingPickerViewModel? ViewModel => DataContext as EncodingPickerViewModel;

    public EncodingPickerDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SubscribeToViewModel();
    }

    private void SubscribeToViewModel()
    {
        if (DataContext is EncodingPickerViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            // Generar columnas para el estado inicial si ya hay campos cargados
            if (vm.PreviewFields.Count > 0)
                GeneratePreviewColumns(vm.PreviewFields);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EncodingPickerViewModel.PreviewFields) && ViewModel is { } vm)
            GeneratePreviewColumns(vm.PreviewFields);
    }

    /// <summary>
    /// Genera las columnas del DataGrid de preview a partir de la lista de campos DBF.
    /// </summary>
    private void GeneratePreviewColumns(IReadOnlyList<DbfField> fields)
    {
        PreviewDataGrid.Columns.Clear();

        foreach (var field in fields)
        {
            var col = new DataGridTextColumn
            {
                Header = field.Name,
                Binding = new Binding
                {
                    Path = new PropertyPath($"Values[{field.Name}]")
                },
                MinWidth = 40,
                MaxWidth = 200,
                Width = DataGridLength.Auto
            };

            PreviewDataGrid.Columns.Add(col);
        }
    }

    private void BtnAceptar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
