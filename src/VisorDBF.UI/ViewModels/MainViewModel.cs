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

    private DbfFile? _currentFile;
    private IReadOnlyList<DbfRecord> _records = Array.Empty<DbfRecord>();
    private IReadOnlyList<DbfField> _fields = Array.Empty<DbfField>();
    private bool _isLoading;
    private string _statusMessage = "Sin archivo";
    private Encoding _activeEncoding = Encoding.GetEncoding("windows-1252");
    private ExportConfiguration _currentExportConfig = ExportConfiguration.Default;

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

    public bool HasFile => CurrentFile != null;
    public string WindowTitle => CurrentFile != null ? $"{CurrentFile.FileName} — VisorDBF" : "VisorDBF";

    public ICommand OpenFileCommand { get; }
    public ICommand ChangeEncodingCommand { get; }
    public ICommand OpenExportConfigCommand { get; }

    public MainViewModel(IDbfReaderService dbfReaderService, IEncodingDetectionService encodingDetectionService)
    {
        _dbfReaderService = dbfReaderService ?? throw new ArgumentNullException(nameof(dbfReaderService));
        _encodingDetectionService = encodingDetectionService ?? throw new ArgumentNullException(nameof(encodingDetectionService));

        OpenFileCommand = new RelayCommand(async _ => await OpenFileAsync(), _ => !IsLoading);
        ChangeEncodingCommand = new RelayCommand(
            async _ => await ChangeEncodingAsync(),
            _ => CurrentFile != null && !IsLoading);
        OpenExportConfigCommand = new RelayCommand(
            async _ => await OpenExportConfigAsync(),
            _ => !IsLoading);
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
            result => CurrentExportConfig = result);

        var dialog = new ExportConfigurationDialog { DataContext = configVm };
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
        }
        await Task.CompletedTask;
    }
}
