using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Models;
using VisorDBF.Core.Services;
using VisorDBF.UI.Views;

namespace VisorDBF.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IDbfReaderService _dbfReaderService;
    private readonly IEncodingDetectionService _encodingDetectionService;
    private readonly IExportService _exportService;
    private readonly IExportService _sqlExportService;
    private readonly ISybaseExportService _sybaseExportService;
    private readonly IColumnFormatService _columnFormatService;
    private readonly ISettingsService _settingsService;

    private DbfFile? _currentFile;
    private IReadOnlyList<DbfRecord> _records = Array.Empty<DbfRecord>();
    private IReadOnlyList<DbfField> _fields = Array.Empty<DbfField>();
    private bool _isLoading;
    private string _statusMessage = "Sin archivo";
    private Encoding _activeEncoding = Encoding.GetEncoding("windows-1252");
    private ExportConfiguration _currentExportConfig = ExportConfiguration.Default;
    private bool _isExporting;
    private CancellationTokenSource? _exportCts;
    private double _exportProgressPercent;
    private bool _areFormatsActive;
    private ColumnFormatConfiguration _currentColumnFormats = ColumnFormatConfiguration.Default;
    private SybaseConnectionConfig? _sybaseConfig;
    private ApplicationSettings _appSettings = ApplicationSettings.Default;
    private string? _activeProfileName;

    public DbfFile? CurrentFile
    {
        get => _currentFile;
        private set
        {
            if (SetField(ref _currentFile, value))
            {
                OnPropertyChanged(nameof(HasFile));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public IReadOnlyList<DbfRecord> Records
    {
        get => _records;
        private set => SetField(ref _records, value);
    }

    public IReadOnlyList<DbfField> Fields
    {
        get => _fields;
        private set => SetField(ref _fields, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public Encoding ActiveEncoding
    {
        get => _activeEncoding;
        private set => SetField(ref _activeEncoding, value);
    }

    public ExportConfiguration CurrentExportConfig
    {
        get => _currentExportConfig;
        set => SetField(ref _currentExportConfig, value);
    }

    public bool IsExporting
    {
        get => _isExporting;
        private set
        {
            if (SetField(ref _isExporting, value))
            {
                OnPropertyChanged(nameof(CanExport));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public double ExportProgressPercent
    {
        get => _exportProgressPercent;
        private set => SetField(ref _exportProgressPercent, value);
    }

    public bool HasFile => CurrentFile != null;
    public string WindowTitle => CurrentFile != null ? $"{CurrentFile.FileName} — VisorDBF" : "VisorDBF";
    public bool CanExport => HasFile && !IsLoading && !IsExporting;

    public event EventHandler? ColumnFormatsChanged;

    public SybaseConnectionConfig? SybaseConfig
    {
        get => _sybaseConfig;
        set
        {
            if (SetField(ref _sybaseConfig, value))
                OnPropertyChanged(nameof(CanTransferToSybase));
        }
    }

    public bool CanTransferToSybase => SybaseConfig?.IsValid == true && HasFile && !IsExporting;

    public ApplicationSettings Settings => _appSettings;

    public string? ActiveProfileName
    {
        get => _activeProfileName;
        set => SetField(ref _activeProfileName, value);
    }

    public bool AreFormatsActive
    {
        get => _areFormatsActive;
        set
        {
            if (SetField(ref _areFormatsActive, value))
                ColumnFormatsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ColumnFormatConfiguration CurrentColumnFormats
    {
        get => _currentColumnFormats;
        set
        {
            if (SetField(ref _currentColumnFormats, value))
                ColumnFormatsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ICommand OpenFileCommand { get; }
    public ICommand ChangeEncodingCommand { get; }
    public ICommand OpenExportConfigCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ExportToSqlCommand { get; }
    public ICommand CancelExportCommand { get; }
    public ICommand OpenColumnFormatsCommand { get; }
    public ICommand ToggleFormatsCommand { get; }
    public ICommand ConfigureSybaseCommand { get; }
    public ICommand TransferToSybaseCommand { get; }

    public MainViewModel(
        IDbfReaderService dbfReaderService,
        IEncodingDetectionService encodingDetectionService,
        IExportService exportService,
        IExportService sqlExportService,
        ISybaseExportService sybaseExportService,
        IColumnFormatService columnFormatService,
        ISettingsService? settingsService = null,
        ApplicationSettings? appSettings = null)
    {
        _dbfReaderService = dbfReaderService ?? throw new ArgumentNullException(nameof(dbfReaderService));
        _encodingDetectionService = encodingDetectionService ?? throw new ArgumentNullException(nameof(encodingDetectionService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _sqlExportService = sqlExportService ?? throw new ArgumentNullException(nameof(sqlExportService));
        _sybaseExportService = sybaseExportService ?? throw new ArgumentNullException(nameof(sybaseExportService));
        _columnFormatService = columnFormatService ?? throw new ArgumentNullException(nameof(columnFormatService));
        _settingsService = settingsService ?? new JsonSettingsService();
        _appSettings = appSettings ?? _settingsService.Load();

        if (_appSettings.LastProfileName != null)
        {
            var profile = _appSettings.Profiles
                .FirstOrDefault(p => p.Name == _appSettings.LastProfileName);
            if (profile != null)
            {
                _currentExportConfig = profile.Config;
                _currentColumnFormats = profile.ColumnFormats;
                _activeProfileName = profile.Name;
            }
        }

        OpenFileCommand = new RelayCommand(async _ => await OpenFileAsync(), _ => !IsLoading);
        ChangeEncodingCommand = new RelayCommand(
            async _ => await ChangeEncodingAsync(),
            _ => CurrentFile != null && !IsLoading);
        OpenExportConfigCommand = new RelayCommand(
            async _ => await OpenExportConfigAsync(),
            _ => !IsLoading);
        ExportCommand = new RelayCommand(async _ => await ExportAsync(), _ => CanExport);
        ExportToSqlCommand = new RelayCommand(async _ => await ExportToSqlAsync(), _ => CanExport);
        CancelExportCommand = new RelayCommand(_ => CancelExport(), _ => IsExporting);
        OpenColumnFormatsCommand = new RelayCommand(
            async _ => await OpenColumnFormatsAsync(),
            _ => HasFile && !IsLoading);
        ToggleFormatsCommand = new RelayCommand(
            _ => AreFormatsActive = !AreFormatsActive,
            _ => HasFile);
        ConfigureSybaseCommand = new RelayCommand(
            async _ => await ConfigureSybaseAsync(),
            _ => !IsLoading);
        TransferToSybaseCommand = new RelayCommand(
            async _ => await TransferToSybaseAsync(),
            _ => CanTransferToSybase);
    }

    private async Task OpenFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Abrir archivo DBF",
            Filter = "Archivos DBF (*.dbf)|*.dbf|Todos los archivos (*.*)|*.*",
            FilterIndex = 1
        };

        if (dialog.ShowDialog() != true) return;

        var filePath = dialog.FileName;
        var prevStatus = StatusMessage;
        IsLoading = true;
        StatusMessage = "Cargando...";

        try
        {
            var languageDriverId = _encodingDetectionService.ReadLanguageDriverId(filePath);
            var detectedEncoding = _encodingDetectionService.DetectEncoding(languageDriverId);

            Encoding encodingToUse;

            if (detectedEncoding == null)
            {
                var pickerVm = new EncodingPickerViewModel(filePath, null)
                {
                    WarningMessage = $"No se pudo detectar la codificacion automaticamente (Language Driver ID: 0x{languageDriverId:X2}). Seleccione la codificacion correcta:"
                };
                var pickerDialog = new EncodingPickerDialog { DataContext = pickerVm };
                var result = pickerDialog.ShowDialog();
                if (result != true)
                {
                    StatusMessage = prevStatus;
                    return;
                }
                encodingToUse = pickerVm.SelectedEncoding;
            }
            else
            {
                encodingToUse = detectedEncoding;
            }

            var dbfFile = await Task.Run(() =>
                _dbfReaderService.ReadAsync(filePath, encodingToUse));

            CurrentFile = dbfFile;
            Fields = dbfFile.Fields;
            Records = dbfFile.Records;
            ActiveEncoding = encodingToUse;
            CurrentColumnFormats = ColumnFormatConfiguration.Default;
            AreFormatsActive = false;
            StatusMessage = $"{dbfFile.RecordCount} registros";
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(HasFile));
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operacion cancelada.";
        }
        catch (DbfReadException ex)
        {
            MessageBox.Show(ex.Message, "Error al abrir archivo",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Error al abrir archivo.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ChangeEncodingAsync()
    {
        if (CurrentFile == null) return;

        var pickerVm = new EncodingPickerViewModel(CurrentFile.FilePath, ActiveEncoding);
        var dialog = new EncodingPickerDialog { DataContext = pickerVm };
        var result = dialog.ShowDialog();
        if (result != true) return;

        IsLoading = true;
        StatusMessage = "Recargando con nueva codificacion...";
        try
        {
            var dbfFile = await Task.Run(() =>
                _dbfReaderService.ReadAsync(CurrentFile.FilePath, pickerVm.SelectedEncoding));
            CurrentFile = dbfFile;
            Fields = dbfFile.Fields;
            Records = dbfFile.Records;
            ActiveEncoding = pickerVm.SelectedEncoding;
            StatusMessage = $"{dbfFile.RecordCount} registros";
            OnPropertyChanged(nameof(WindowTitle));
        }
        catch (DbfReadException ex)
        {
            MessageBox.Show(ex.Message, "Error al recargar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsLoading = false; }
    }

    private async Task OpenExportConfigAsync()
    {
        var configVm = new ExportConfigurationViewModel(
            CurrentExportConfig,
            result =>
            {
                CurrentExportConfig = result;
                SaveSettings();
            },
            _appSettings.Profiles,
            ActiveProfileName);

        configVm.ProfilesChanged += () =>
        {
            _appSettings = _appSettings with { Profiles = configVm.Profiles };
            SaveSettings();
        };

        var dialog = new ExportConfigurationDialog { DataContext = configVm };
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            ActiveProfileName = configVm.SelectedProfile?.Name;
        }
        await Task.CompletedTask;
    }

    private async Task OpenColumnFormatsAsync()
    {
        if (CurrentFile == null) return;

        var firstRecord = Records.Count > 0 ? Records[0] : null;
        var configVm = new ColumnFormatsViewModel(
            Fields,
            firstRecord,
            CurrentColumnFormats,
            _columnFormatService,
            result =>
            {
                CurrentColumnFormats = result;
                AreFormatsActive = result.IsActive;
            });

        var dialog = new ColumnFormatsWindow { DataContext = configVm };
        dialog.Owner = Application.Current.MainWindow;
        dialog.ShowDialog();
        await Task.CompletedTask;
    }

    private async Task ExportAsync()
    {
        if (CurrentFile == null) return;

        var saveDialog = new SaveFileDialog
        {
            Title = "Guardar archivo de exportacion",
            Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
            DefaultExt = ".txt",
            InitialDirectory = Path.GetDirectoryName(CurrentFile.FilePath),
            FileName = Path.GetFileNameWithoutExtension(CurrentFile.FilePath) + ".txt"
        };

        if (saveDialog.ShowDialog() != true) return;

        _exportCts = new CancellationTokenSource();
        var progressVm = new ExportProgressDialogViewModel(
            CurrentFile.RecordCount, saveDialog.FileName, () => _exportCts?.Cancel());
        var progressDialog = new ExportProgressDialog { DataContext = progressVm };
        progressDialog.Owner = Application.Current.MainWindow;

        var syncContext = SynchronizationContext.Current;
        var totalRecords = CurrentFile.RecordCount;

        // Crear Progress<int> en el UI thread para que capture el SynchronizationContext de la UI
        var progress = new Progress<int>(processed =>
        {
            progressVm.ProcessedRecords = processed;
            ExportProgressPercent = (double)processed / totalRecords * 100;
        });

        try
        {
            IsExporting = true;
            ExportProgressPercent = 0;

            var exportTask = Task.Run(async () =>
            {
                try
                {
                    await _exportService.ExportAsync(
                        CurrentFile,
                        CurrentExportConfig,
                        saveDialog.FileName,
                        progress,
                        _exportCts.Token,
                        CurrentColumnFormats);

                    syncContext?.Post(_ => progressVm.IsComplete = true, null);
                }
                catch (OperationCanceledException)
                {
                    syncContext?.Post(_ => progressVm.IsCancelled = true, null);
                    throw;
                }
            });

            progressDialog.ShowDialog();

            await exportTask;
        }
        catch (OperationCanceledException)
        {
            // Ya se manejó dentro del Task.Run (IsCancelled = true en el diálogo)
        }
        catch (ExportException ex)
        {
            MessageBox.Show(ex.Message, "Error de exportacion",
                MessageBoxButton.OK, MessageBoxImage.Error);
            if (progressDialog.IsVisible)
                progressDialog.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Se produjo un error durante la exportacion. El archivo parcial fue eliminado.\n\n{ex.Message}",
                "Error de exportacion", MessageBoxButton.OK, MessageBoxImage.Error);
            if (progressDialog.IsVisible)
                progressDialog.Close();
        }
        finally
        {
            IsExporting = false;
            _exportCts?.Dispose();
            _exportCts = null;
        }
    }

    private async Task ExportToSqlAsync()
    {
        if (CurrentFile == null) return;

        var saveDialog = new SaveFileDialog
        {
            Title = "Guardar archivo SQL",
            Filter = "Archivos SQL (*.sql)|*.sql|Todos los archivos (*.*)|*.*",
            DefaultExt = ".sql",
            InitialDirectory = Path.GetDirectoryName(CurrentFile.FilePath),
            FileName = Path.GetFileNameWithoutExtension(CurrentFile.FilePath) + ".sql"
        };

        if (saveDialog.ShowDialog() != true) return;

        _exportCts = new CancellationTokenSource();
        var progressVm = new ExportProgressDialogViewModel(
            CurrentFile.RecordCount, saveDialog.FileName, () => _exportCts?.Cancel());
        var progressDialog = new ExportProgressDialog { DataContext = progressVm };
        progressDialog.Owner = Application.Current.MainWindow;

        var syncContext = SynchronizationContext.Current;
        var totalRecords = CurrentFile.RecordCount;

        var progress = new Progress<int>(processed =>
        {
            progressVm.ProcessedRecords = processed;
            ExportProgressPercent = (double)processed / totalRecords * 100;
        });

        try
        {
            IsExporting = true;
            ExportProgressPercent = 0;

            var exportTask = Task.Run(async () =>
            {
                try
                {
                    await _sqlExportService.ExportAsync(
                        CurrentFile,
                        CurrentExportConfig,
                        saveDialog.FileName,
                        progress,
                        _exportCts.Token);

                    syncContext?.Post(_ => progressVm.IsComplete = true, null);
                }
                catch (OperationCanceledException)
                {
                    syncContext?.Post(_ => progressVm.IsCancelled = true, null);
                    throw;
                }
            });

            progressDialog.ShowDialog();
            await exportTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (ExportException ex)
        {
            MessageBox.Show(ex.Message, "Error de exportacion SQL",
                MessageBoxButton.OK, MessageBoxImage.Error);
            if (progressDialog.IsVisible)
                progressDialog.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Se produjo un error durante la exportacion SQL. El archivo parcial fue eliminado.\n\n{ex.Message}",
                "Error de exportacion SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            if (progressDialog.IsVisible)
                progressDialog.Close();
        }
        finally
        {
            IsExporting = false;
            _exportCts?.Dispose();
            _exportCts = null;
        }
    }

    private async Task ConfigureSybaseAsync()
    {
        var configVm = new SybaseConnectionViewModel(
            _sybaseConfig,
            result =>
            {
                SybaseConfig = result;
                CommandManager.InvalidateRequerySuggested();
            });

        var dialog = new SybaseConnectionDialog { DataContext = configVm };
        dialog.Owner = Application.Current.MainWindow;
        dialog.ShowDialog();
        await Task.CompletedTask;
    }

    private async Task TransferToSybaseAsync()
    {
        if (CurrentFile == null || _sybaseConfig == null) return;

        _exportCts = new CancellationTokenSource();
        var progressVm = new ExportProgressDialogViewModel(
            CurrentFile.RecordCount, _sybaseConfig.TableName, () => _exportCts?.Cancel());
        var progressDialog = new ExportProgressDialog { DataContext = progressVm };
        progressDialog.Owner = Application.Current.MainWindow;

        var syncContext = SynchronizationContext.Current;
        var totalRecords = CurrentFile.RecordCount;

        var progress = new Progress<int>(processed =>
        {
            progressVm.ProcessedRecords = processed;
            ExportProgressPercent = (double)processed / totalRecords * 100;
        });

        try
        {
            IsExporting = true;
            ExportProgressPercent = 0;

            var transferTask = Task.Run(async () =>
            {
                try
                {
                    var probeOk = await _sybaseExportService.ProbeFirstRecordAsync(
                        CurrentFile, _sybaseConfig, _exportCts.Token);

                    if (!probeOk)
                    {
                        syncContext?.Post(_ =>
                        {
                            MessageBox.Show("El probe de conversiones fallo. Revise el log para mas detalles.",
                                "Error de traspaso Sybase", MessageBoxButton.OK, MessageBoxImage.Error);
                            progressDialog.Close();
                        }, null);
                        return;
                    }

                    await _sybaseExportService.TransferAsync(
                        CurrentFile,
                        _sybaseConfig,
                        progress,
                        _exportCts.Token);

                    syncContext?.Post(_ => progressVm.IsComplete = true, null);
                }
                catch (OperationCanceledException)
                {
                    syncContext?.Post(_ => progressVm.IsCancelled = true, null);
                    throw;
                }
                catch (ExportException ex)
                {
                    syncContext?.Post(_ =>
                    {
                        MessageBox.Show(ex.Message, "Error de traspaso Sybase",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        progressDialog.Close();
                    }, null);
                }
                catch (Exception ex)
                {
                    syncContext?.Post(_ =>
                    {
                        MessageBox.Show($"Se produjo un error durante el traspaso a Sybase.\n\n{ex.Message}",
                            "Error de traspaso Sybase", MessageBoxButton.OK, MessageBoxImage.Error);
                        progressDialog.Close();
                    }, null);
                }
            });

            progressDialog.ShowDialog();
            await transferTask;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsExporting = false;
            _exportCts?.Dispose();
            _exportCts = null;
        }
    }

    private void SaveSettings()
    {
        if (_settingsService == null) return;

        _appSettings = _appSettings with
        {
            DefaultExportConfig = CurrentExportConfig,
            LastProfileName = ActiveProfileName
        };

        _settingsService.Save(_appSettings);
    }

    public void SaveWindowSettings(WindowSettings ws)
    {
        _appSettings = _appSettings with { WindowState = ws };
    }

    public void SaveSettingsOnClose()
    {
        _appSettings = _appSettings with
        {
            DefaultExportConfig = CurrentExportConfig,
            LastProfileName = ActiveProfileName,
            Profiles = _appSettings.Profiles,
            RecentFiles = _appSettings.RecentFiles,
            WindowState = _appSettings.WindowState
        };

        _settingsService.Save(_appSettings);
    }

    private void CancelExport()
    {
        _exportCts?.Cancel();
    }
}
