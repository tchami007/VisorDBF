using System.Text;
using System.Windows.Input;
using VisorDBF.Core.Models;
namespace VisorDBF.UI.ViewModels;

public class ExportConfigurationViewModel : ViewModelBase
{
    private static readonly string[] PriorityOutputEncodings = [
        "utf-8", "windows-1252", "iso-8859-1", "utf-16", "ibm850"
    ];

    private string _columnSeparator = ";";
    private bool _useCustomSeparator;
    private string _customSeparatorChar = string.Empty;
    private bool _includeHeader = true;
    private bool _exportAllRows = true;
    private int _maxRows = 1000;
    private string _rowEndDelimiter = string.Empty;
    private Encoding _outputEncoding = Encoding.UTF8;

    public string ColumnSeparator
    {
        get => _columnSeparator;
        set
        {
            if (SetField(ref _columnSeparator, value))
                OnPropertyChanged(nameof(SeparatorPreview));
        }
    }

    public bool UseCustomSeparator
    {
        get => _useCustomSeparator;
        set
        {
            if (SetField(ref _useCustomSeparator, value))
                OnPropertyChanged(nameof(SeparatorPreview));
        }
    }

    public string CustomSeparatorChar
    {
        get => _customSeparatorChar;
        set
        {
            if (SetField(ref _customSeparatorChar, value))
                OnPropertyChanged(nameof(SeparatorPreview));
        }
    }

    public bool IncludeHeader
    {
        get => _includeHeader;
        set => SetField(ref _includeHeader, value);
    }

    public bool ExportAllRows
    {
        get => _exportAllRows;
        set => SetField(ref _exportAllRows, value);
    }

    public int MaxRows
    {
        get => _maxRows;
        set => SetField(ref _maxRows, value);
    }

    public string RowEndDelimiter
    {
        get => _rowEndDelimiter;
        set
        {
            if (SetField(ref _rowEndDelimiter, value))
                OnPropertyChanged(nameof(SeparatorPreview));
        }
    }

    public Encoding OutputEncoding
    {
        get => _outputEncoding;
        set => SetField(ref _outputEncoding, value);
    }

    public string SeparatorPreview
    {
        get
        {
            var sep = _useCustomSeparator ? _customSeparatorChar : _columnSeparator;
            var preview = $"CAMPO1{sep}CAMPO2{sep}CAMPO3";
            if (!string.IsNullOrEmpty(_rowEndDelimiter))
                preview += _rowEndDelimiter;
            return $"Previa: {preview}";
        }
    }

    public IReadOnlyList<EncodingItem> AvailableOutputEncodings { get; }

    public ICommand ApplyCommand { get; }

    public ExportConfiguration? Result { get; private set; }

    private readonly Action<ExportConfiguration> _applyAction;

    public ExportConfigurationViewModel(ExportConfiguration current, Action<ExportConfiguration> applyAction)
    {
        _applyAction = applyAction;
        _columnSeparator = current.ColumnSeparator;
        _includeHeader = current.IncludeHeader;
        _exportAllRows = current.RowLimitMode == RowLimitMode.All;
        _maxRows = current.MaxRows;
        _rowEndDelimiter = current.RowEndDelimiter;
        _outputEncoding = current.OutputEncoding;
        _useCustomSeparator = !IsBuiltInSeparator(current.ColumnSeparator);
        if (_useCustomSeparator)
            _customSeparatorChar = current.ColumnSeparator;
        AvailableOutputEncodings = BuildEncodingList();
        ApplyCommand = new RelayCommand(_ => Apply());
    }

    private static bool IsBuiltInSeparator(string sep) => sep switch
    {
        "," => true,
        ";" => true,
        "\t" => true,
        "|" => true,
        _ => false
    };

    public ExportConfiguration BuildResult()
    {
        var sep = _useCustomSeparator ? _customSeparatorChar : _columnSeparator;
        return new ExportConfiguration
        {
            ColumnSeparator = sep,
            RowEndDelimiter = _rowEndDelimiter,
            IncludeHeader = _includeHeader,
            RowLimitMode = _exportAllRows ? RowLimitMode.All : RowLimitMode.FirstN,
            MaxRows = _exportAllRows ? 0 : _maxRows,
            OutputEncoding = _outputEncoding
        };
    }

    public void Apply()
    {
        Result = BuildResult();
        _applyAction(Result);
    }

    private static IReadOnlyList<EncodingItem> BuildEncodingList()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var priority = PriorityOutputEncodings
            .Select(name => TryGetEncoding(name))
            .OfType<Encoding>()
            .Select(e => new EncodingItem(e));

        var rest = Encoding.GetEncodings()
            .Where(ei => !PriorityOutputEncodings.Contains(ei.Name, StringComparer.OrdinalIgnoreCase))
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
