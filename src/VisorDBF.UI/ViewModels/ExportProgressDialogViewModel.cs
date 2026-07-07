using System.Diagnostics;
using System.Windows.Input;
namespace VisorDBF.UI.ViewModels;

public class ExportProgressDialogViewModel : ViewModelBase
{
    private int _processedRecords;
    private bool _isComplete;
    private bool _isCancelled;

    public int TotalRecords { get; }
    public string OutputPath { get; }

    public int ProcessedRecords
    {
        get => _processedRecords;
        set
        {
            if (SetField(ref _processedRecords, value))
            {
                OnPropertyChanged(nameof(ProgressPercent));
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public double ProgressPercent => TotalRecords > 0
        ? (double)_processedRecords / TotalRecords * 100
        : 0;

    public string ProgressText =>
        $"Registros procesados: {_processedRecords:N0} / {TotalRecords:N0}";

    public bool IsComplete
    {
        get => _isComplete;
        set
        {
            if (SetField(ref _isComplete, value))
            {
                OnPropertyChanged(nameof(IsExporting));
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(ShowProgress));
            }
        }
    }

    public bool IsCancelled
    {
        get => _isCancelled;
        set
        {
            if (SetField(ref _isCancelled, value))
            {
                OnPropertyChanged(nameof(IsExporting));
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(ShowProgress));
            }
        }
    }

    public bool IsExporting => !IsComplete && !IsCancelled;
    public bool ShowProgress => !IsComplete && !IsCancelled;

    public string WindowTitle => IsComplete
        ? "Exportacion completada"
        : IsCancelled ? "Exportacion cancelada" : "Exportando...";

    public string ExportCountText => $"{_processedRecords:N0} registros exportados";

    public ICommand CancelCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand CloseCommand { get; }

    private readonly Action _cancelAction;

    public ExportProgressDialogViewModel(int totalRecords, string outputPath, Action cancelAction)
    {
        TotalRecords = totalRecords;
        OutputPath = outputPath;
        _cancelAction = cancelAction;

        CancelCommand = new RelayCommand(_ => _cancelAction());
        OpenFolderCommand = new RelayCommand(_ => OpenFolder());
        CloseCommand = new RelayCommand(_ => { });
    }

    private void OpenFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{OutputPath}\"",
            UseShellExecute = true
        });
    }
}
