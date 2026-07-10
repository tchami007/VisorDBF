using System.Text;
using System.Windows;
using System.Windows.Input;
using VisorDBF.Core.Models;
using VisorDBF.UI.Views;
namespace VisorDBF.UI.ViewModels;

public class ExportConfigurationViewModel : ViewModelBase
{
    private static readonly string[] PriorityOutputEncodings = [
        "utf-8", "windows-1252", "iso-8859-1", "utf-16", "ibm850"
    ];

    private string _columnSeparator = ";";
    private bool _useCustomSeparator;
    private string _customSeparator = string.Empty;
    private bool _includeHeader = true;
    private bool _exportAllRows = true;
    private int _maxRows = 1000;
    private string _rowEndDelimiter = string.Empty;
    private Encoding _outputEncoding = ExportConfiguration.UTF8NoBOM;
    private string _decimalSeparator = ",";
    private List<ExportProfile> _profiles = new();
    private ExportProfile? _selectedProfile;

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

    public string CustomSeparator
    {
        get => _customSeparator;
        set
        {
            if (SetField(ref _customSeparator, value))
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
            var sep = _useCustomSeparator ? _customSeparator : _columnSeparator;
            var preview = $"CAMPO1{sep}CAMPO2{sep}CAMPO3";
            if (!string.IsNullOrEmpty(_rowEndDelimiter))
                preview += _rowEndDelimiter;
            return $"Previa: {preview}";
        }
    }

    public string DecimalSeparator
    {
        get => _decimalSeparator;
        set => SetField(ref _decimalSeparator, value);
    }

    public bool UseCommaDecimal
    {
        get => _decimalSeparator == ",";
        set
        {
            if (value) _decimalSeparator = ",";
            OnPropertyChanged(nameof(UseCommaDecimal));
            OnPropertyChanged(nameof(UseDotDecimal));
        }
    }

    public bool UseDotDecimal
    {
        get => _decimalSeparator == ".";
        set
        {
            if (value) _decimalSeparator = ".";
            OnPropertyChanged(nameof(UseCommaDecimal));
            OnPropertyChanged(nameof(UseDotDecimal));
        }
    }

    public IReadOnlyList<EncodingItem> AvailableOutputEncodings { get; }

    public ICommand ApplyCommand { get; }
    public ICommand SaveAsProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }
    public ICommand RenameProfileCommand { get; }

    public ExportConfiguration? Result { get; private set; }

    private readonly Action<ExportConfiguration> _applyAction;

    public List<ExportProfile> Profiles
    {
        get => _profiles;
        set => SetField(ref _profiles, value);
    }

    public ExportProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetField(ref _selectedProfile, value))
            {
                if (value != null)
                    ApplyProfile(value);
                OnPropertyChanged(nameof(IsProfileSelected));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsProfileSelected => SelectedProfile != null;

    public event Action? ProfilesChanged;

    public ExportConfigurationViewModel(
        ExportConfiguration current,
        Action<ExportConfiguration> applyAction,
        List<ExportProfile>? profiles = null,
        string? lastProfileName = null)
    {
        _applyAction = applyAction;
        _columnSeparator = current.ColumnSeparator;
        _includeHeader = current.IncludeHeader;
        _exportAllRows = current.RowLimitMode == RowLimitMode.All;
        _maxRows = current.MaxRows > 0 ? current.MaxRows : 1000;
        _rowEndDelimiter = current.RowEndDelimiter;
        _outputEncoding = Equals(current.OutputEncoding, Encoding.UTF8)
            ? ExportConfiguration.UTF8NoBOM
            : current.OutputEncoding;
        _decimalSeparator = current.DecimalSeparator;
        _useCustomSeparator = !IsBuiltInSeparator(current.ColumnSeparator);
        if (_useCustomSeparator)
            _customSeparator = current.ColumnSeparator;
        AvailableOutputEncodings = BuildEncodingList();
        ApplyCommand = new RelayCommand(_ => Apply());

        if (profiles != null)
            _profiles = profiles;

        SaveAsProfileCommand = new RelayCommand(async _ => await SaveAsProfileAsync());
        DeleteProfileCommand = new RelayCommand(
            async _ => await DeleteProfileAsync(),
            _ => IsProfileSelected);
        RenameProfileCommand = new RelayCommand(
            async _ => await RenameProfileAsync(),
            _ => IsProfileSelected);

        if (lastProfileName != null)
        {
            SelectedProfile = _profiles.FirstOrDefault(p => p.Name == lastProfileName);
        }
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
        var sep = _useCustomSeparator ? _customSeparator : _columnSeparator;
        return new ExportConfiguration
        {
            ColumnSeparator = sep,
            RowEndDelimiter = _rowEndDelimiter,
            IncludeHeader = _includeHeader,
            RowLimitMode = _exportAllRows ? RowLimitMode.All : RowLimitMode.FirstN,
            MaxRows = _maxRows,
            OutputEncoding = _outputEncoding,
            DecimalSeparator = _decimalSeparator
        };
    }

    public void Apply()
    {
        Result = BuildResult();
        _applyAction(Result);
    }

    private void ApplyProfile(ExportProfile profile)
    {
        _columnSeparator = profile.Config.ColumnSeparator;
        _includeHeader = profile.Config.IncludeHeader;
        _exportAllRows = profile.Config.RowLimitMode == RowLimitMode.All;
        _maxRows = profile.Config.MaxRows > 0 ? profile.Config.MaxRows : 1000;
        _rowEndDelimiter = profile.Config.RowEndDelimiter;
        _outputEncoding = Equals(profile.Config.OutputEncoding, Encoding.UTF8)
            ? ExportConfiguration.UTF8NoBOM
            : profile.Config.OutputEncoding;
        _decimalSeparator = profile.Config.DecimalSeparator;
        _useCustomSeparator = !IsBuiltInSeparator(profile.Config.ColumnSeparator);
        _customSeparator = _useCustomSeparator ? profile.Config.ColumnSeparator : string.Empty;

        OnPropertyChanged(nameof(ColumnSeparator));
        OnPropertyChanged(nameof(IncludeHeader));
        OnPropertyChanged(nameof(ExportAllRows));
        OnPropertyChanged(nameof(MaxRows));
        OnPropertyChanged(nameof(RowEndDelimiter));
        OnPropertyChanged(nameof(OutputEncoding));
        OnPropertyChanged(nameof(DecimalSeparator));
        OnPropertyChanged(nameof(UseCustomSeparator));
        OnPropertyChanged(nameof(CustomSeparator));
        OnPropertyChanged(nameof(UseCommaDecimal));
        OnPropertyChanged(nameof(UseDotDecimal));
        OnPropertyChanged(nameof(SeparatorPreview));
    }

    private ExportProfile CreateProfileFromCurrent(string name)
    {
        return new ExportProfile
        {
            Name = name,
            Config = new ExportConfiguration
            {
                ColumnSeparator = _useCustomSeparator ? _customSeparator : _columnSeparator,
                RowEndDelimiter = _rowEndDelimiter,
                IncludeHeader = _includeHeader,
                RowLimitMode = _exportAllRows ? RowLimitMode.All : RowLimitMode.FirstN,
                MaxRows = _maxRows,
                OutputEncoding = _outputEncoding,
                DecimalSeparator = _decimalSeparator,
                ColumnFormats = ColumnFormatConfiguration.Default
            },
            ColumnFormats = ColumnFormatConfiguration.Default
        };
    }

    private async Task SaveAsProfileAsync()
    {
        var existingNames = Profiles.Select(p => p.Name).ToList();
        var vm = new SaveProfileViewModel(null, existingNames);
        var dialog = new SaveProfileDialog { DataContext = vm };
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            var name = vm.ProfileName.Trim();
            var existing = Profiles.FirstOrDefault(p => p.Name == name);

            if (existing != null)
            {
                var result = MessageBox.Show(
                    $"Ya existe un perfil llamado \"{name}\". Desea sobrescribirlo?",
                    "Perfil existente",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;
                Profiles.Remove(existing);
            }

            var profile = CreateProfileFromCurrent(name);
            Profiles = new List<ExportProfile>(Profiles) { profile };
            SelectedProfile = profile;
            OnProfilesChanged();
        }
        await Task.CompletedTask;
    }

    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile == null) return;

        var result = MessageBox.Show(
            $"Desea eliminar el perfil \"{SelectedProfile.Name}\"?",
            "Eliminar perfil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var updated = new List<ExportProfile>(Profiles);
            updated.Remove(SelectedProfile);
            Profiles = updated;
            SelectedProfile = Profiles.LastOrDefault();
            OnProfilesChanged();
        }
        await Task.CompletedTask;
    }

    private async Task RenameProfileAsync()
    {
        if (SelectedProfile == null) return;

        var existingNames = Profiles
            .Where(p => p.Name != SelectedProfile.Name)
            .Select(p => p.Name)
            .ToList();

        var vm = new SaveProfileViewModel(SelectedProfile.Name, existingNames);
        var dialog = new SaveProfileDialog { DataContext = vm };
        dialog.Owner = Application.Current.MainWindow;
        dialog.Title = "Renombrar perfil";

        if (dialog.ShowDialog() == true)
        {
            var newName = vm.ProfileName.Trim();
            var existing = Profiles.FirstOrDefault(p => p.Name == newName);

            if (existing != null)
            {
                var result = MessageBox.Show(
                    $"Ya existe un perfil llamado \"{newName}\". Desea sobrescribirlo?",
                    "Perfil existente",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;
                Profiles.Remove(existing);
            }

            var profile = CreateProfileFromCurrent(newName);
            var updated = new List<ExportProfile>(Profiles);
            updated.Remove(SelectedProfile);
            updated.Add(profile);
            Profiles = updated;
            SelectedProfile = profile;
            OnProfilesChanged();
        }
        await Task.CompletedTask;
    }

    private void OnProfilesChanged()
    {
        OnPropertyChanged(nameof(Profiles));
        ProfilesChanged?.Invoke();
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
        try
        {
            if (string.Equals(name, "utf-8", StringComparison.OrdinalIgnoreCase))
                return ExportConfiguration.UTF8NoBOM;
            return Encoding.GetEncoding(name);
        }
        catch { return null; }
    }
}
