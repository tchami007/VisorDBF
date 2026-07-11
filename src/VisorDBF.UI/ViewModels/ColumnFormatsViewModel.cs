using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using VisorDBF.Core.Models;
using VisorDBF.Core.Services;
namespace VisorDBF.UI.ViewModels;

public sealed class ColumnFormatsViewModel : ViewModelBase
{
    private static readonly FrozenDictionary<string, string[]> PresetFormats =
        new Dictionary<string, string[]>
    {
        ["Date"] = ["dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "d MMM yyyy"],
        ["DateTime"] = ["dd/MM/yyyy HH:mm", "MM/dd/yyyy HH:mm", "yyyy-MM-dd HH:mm"],
        ["Numeric"] = ["N0", "N2", "C0", "C2", "P0", "P2"],
        ["Float"] = ["F0", "F2", "F4", "E2", "G", "N2"]
    }.ToFrozenDictionary();

    private readonly IColumnFormatService _formatService;
    private readonly DbfRecord? _firstRecord;
    private readonly Action<ColumnFormatConfiguration> _applyAction;
    private ColumnFormatItem? _selectedColumn;

    public ObservableCollection<ColumnFormatItem> Columns { get; }

    public ColumnFormatItem? SelectedColumn
    {
        get => _selectedColumn;
        set
        {
            if (SetField(ref _selectedColumn, value))
            {
                OnPropertyChanged(nameof(HasSelectedFormattableColumn));
                OnPropertyChanged(nameof(CurrentPresets));
            }
        }
    }

    public bool HasSelectedFormattableColumn => SelectedColumn?.IsFormattable == true;

    public IEnumerable<string> CurrentPresets
    {
        get
        {
            if (SelectedColumn?.IsFormattable != true)
                return Enumerable.Empty<string>();
            var typeName = SelectedColumn.FieldType.ToString();
            return PresetFormats.TryGetValue(typeName, out var presets) ? presets : Enumerable.Empty<string>();
        }
    }

    public bool HasInvalidFormat => Columns.Any(c => c.IsFormattable && !c.IsValidFormat && c.IsEnabled);

    public ICommand ApplyPresetCommand { get; }
    public ICommand ClearAllCommand { get; }

    public ColumnFormatsViewModel(
        IReadOnlyList<DbfField> fields,
        DbfRecord? firstRecord,
        ColumnFormatConfiguration currentFormats,
        IColumnFormatService formatService,
        Action<ColumnFormatConfiguration> applyAction)
    {
        _formatService = formatService ?? throw new ArgumentNullException(nameof(formatService));
        _firstRecord = firstRecord;
        _applyAction = applyAction ?? throw new ArgumentNullException(nameof(applyAction));

        Columns = new ObservableCollection<ColumnFormatItem>(
            fields.Select(f => new ColumnFormatItem
            {
                FieldName = f.Name,
                FieldType = f.Type,
                IsFormattable = f.Type.HasConfigurableFormat(),
                FormatString = currentFormats.Formats.GetValueOrDefault(f.Name),
                IsEnabled = currentFormats.Formats.ContainsKey(f.Name),
                FieldTypeDisplay = f.Type.ToDisplayString(),
                FieldHeader = $"{f.Name} ({f.Type.ToDisplayString()})"
            }));

        foreach (var item in Columns)
        {
            item.PropertyChanged += OnItemPropertyChanged;
            if (item.IsFormattable)
                ValidateItem(item);
        }

        ApplyPresetCommand = new RelayCommand(ApplyPreset);
        ClearAllCommand = new RelayCommand(_ => ClearAll());

        if (Columns.Count > 0)
            SelectedColumn = Columns[0];
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ColumnFormatItem item || !item.IsFormattable) return;
        if (e.PropertyName == nameof(ColumnFormatItem.FormatString) ||
            e.PropertyName == nameof(ColumnFormatItem.IsEnabled))
            ValidateItem(item);
    }

    public void ValidateItem(ColumnFormatItem item)
    {
        if (!item.IsFormattable) return;

        item.IsValidFormat = string.IsNullOrEmpty(item.FormatString) ||
            _formatService.IsFormatValid(item.FormatString, item.FieldType);

        if (item.IsEnabled && item.IsValidFormat && _firstRecord != null &&
            _firstRecord.Values.TryGetValue(item.FieldName, out var rawValue))
        {
            item.PreviewValue = _formatService.ApplyFormat(item.FieldName, rawValue, item.FormatString);
        }
        else if (item.IsEnabled)
        {
            item.PreviewValue = item.FormatString;
        }
        else
        {
            item.PreviewValue = null;
        }

        OnPropertyChanged(nameof(HasInvalidFormat));
    }

    private void ApplyPreset(object? parameter)
    {
        if (parameter is not string format || SelectedColumn?.IsFormattable != true) return;
        SelectedColumn.FormatString = format;
        SelectedColumn.IsEnabled = true;
    }

    private void ClearAll()
    {
        foreach (var item in Columns)
        {
            if (!item.IsFormattable) continue;
            item.FormatString = null;
            item.IsEnabled = false;
        }
    }

    public ColumnFormatConfiguration BuildResult()
    {
        var formats = new Dictionary<string, string?>();
        foreach (var item in Columns)
        {
            if (item.IsFormattable && item.IsEnabled)
                formats[item.FieldName] = item.FormatString;
        }
        return new ColumnFormatConfiguration
        {
            Formats = formats,
            IsActive = formats.Count > 0
        };
    }

    public void Apply()
    {
        _applyAction(BuildResult());
    }
}

public sealed class ColumnFormatItem : ViewModelBase
{
    private string? _formatString;
    private string? _previewValue;
    private bool _isEnabled;
    private bool _isValidFormat = true;

    public string FieldName { get; init; } = string.Empty;
    public DbfFieldType FieldType { get; init; }
    public bool IsFormattable { get; init; }
    public string FieldTypeDisplay { get; init; } = string.Empty;
    public string FieldHeader { get; init; } = string.Empty;

    public string? FormatString
    {
        get => _formatString;
        set => SetField(ref _formatString, value);
    }

    public string? PreviewValue
    {
        get => _previewValue;
        set => SetField(ref _previewValue, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    public bool IsValidFormat
    {
        get => _isValidFormat;
        set => SetField(ref _isValidFormat, value);
    }
}
