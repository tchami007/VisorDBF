using System.Text;
using VisorDBF.Core.Models;
using VisorDBF.Core.Services;

namespace VisorDBF.UI.ViewModels;

/// <summary>
/// ViewModel para el dialogo de seleccion de codificacion de lectura.
/// Expone la lista de encodings disponibles, el encoding seleccionado,
/// y una vista previa de los primeros 5 registros del archivo con el encoding activo.
/// </summary>
public sealed class EncodingPickerViewModel : ViewModelBase
{
    // Encodings comunes al inicio de la lista (segun D-12)
    private static readonly string[] PriorityEncodings = [
        "windows-1252", "IBM850", "utf-8", "iso-8859-1",
        "IBM437", "IBM852", "windows-1250", "windows-1251"
    ];

    private Encoding _selectedEncoding;
    private string? _warningMessage;
    private IReadOnlyList<DbfRecord> _previewRecords = Array.Empty<DbfRecord>();
    private IReadOnlyList<DbfField> _previewFields = Array.Empty<DbfField>();
    private bool _isLoadingPreview;
    private readonly string _filePath;

    public IReadOnlyList<EncodingItem> AvailableEncodings { get; }

    public Encoding SelectedEncoding
    {
        get => _selectedEncoding;
        set { SetField(ref _selectedEncoding, value); _ = LoadPreviewAsync(); }
    }

    public string? WarningMessage
    {
        get => _warningMessage;
        set
        {
            if (SetField(ref _warningMessage, value))
                OnPropertyChanged(nameof(HasWarning));
        }
    }

    /// <summary>
    /// True cuando WarningMessage tiene contenido — controla la visibilidad del panel de advertencia.
    /// </summary>
    public bool HasWarning => WarningMessage != null;

    public IReadOnlyList<DbfRecord> PreviewRecords
    {
        get => _previewRecords;
        private set => SetField(ref _previewRecords, value);
    }

    public IReadOnlyList<DbfField> PreviewFields
    {
        get => _previewFields;
        private set => SetField(ref _previewFields, value);
    }

    public bool IsLoadingPreview
    {
        get => _isLoadingPreview;
        private set => SetField(ref _isLoadingPreview, value);
    }

    public EncodingPickerViewModel(string filePath, Encoding? detectedEncoding)
    {
        _filePath = filePath;
        AvailableEncodings = BuildEncodingList();
        _selectedEncoding = detectedEncoding
            ?? Encoding.GetEncoding("windows-1252");

        // Cargar preview inicial con el encoding detectado
        _ = LoadPreviewAsync();
    }

    private async Task LoadPreviewAsync()
    {
        // Cargar los primeros 5 registros con la codificacion seleccionada
        // para mostrar en el DataGrid de preview
        IsLoadingPreview = true;
        try
        {
            var service = new DbfReaderService();
            var file = await service.ReadAsync(_filePath, SelectedEncoding);
            PreviewFields = file.Fields;
            PreviewRecords = file.Records.Take(5).ToList();
        }
        catch
        {
            PreviewRecords = Array.Empty<DbfRecord>();
        }
        finally
        {
            IsLoadingPreview = false;
        }
    }

    private static IReadOnlyList<EncodingItem> BuildEncodingList()
    {
        var priority = PriorityEncodings
            .Select(name => TryGetEncoding(name))
            .OfType<Encoding>()
            .Select(e => new EncodingItem(e));

        var rest = Encoding.GetEncodings()
            .Where(ei => !PriorityEncodings.Contains(ei.Name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(ei => ei.DisplayName)
            .Select(ei => new EncodingItem(ei.GetEncoding()));

        return priority.Concat(rest).ToList();
    }

    private static Encoding? TryGetEncoding(string name)
    {
        try { return Encoding.GetEncoding(name); }
        catch { return null; }
    }
}

/// <summary>
/// Wraps an Encoding with a user-friendly display name for the ComboBox.
/// </summary>
public record EncodingItem(Encoding Encoding)
{
    public string DisplayName => $"{Encoding.WebName} — {Encoding.EncodingName}";
}
