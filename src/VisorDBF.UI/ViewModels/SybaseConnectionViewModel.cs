using System.Data.Odbc;
using System.Windows;
using System.Windows.Input;
using VisorDBF.Core.Models;
namespace VisorDBF.UI.ViewModels;

public sealed class SybaseConnectionViewModel : ViewModelBase
{
    private string _host = string.Empty;
    private int _port = 5000;
    private string _database = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _tableName = string.Empty;
    private string _testMessage = string.Empty;
    private bool _isTestSuccessful;
    private bool _isTesting;
    private string _detailedError = string.Empty;

    public string Host
    {
        get => _host;
        set
        {
            if (SetField(ref _host, value))
                OnPropertyChanged(nameof(ConnectionString));
        }
    }

    public int Port
    {
        get => _port;
        set
        {
            if (SetField(ref _port, value))
                OnPropertyChanged(nameof(ConnectionString));
        }
    }

    public string Database
    {
        get => _database;
        set
        {
            if (SetField(ref _database, value))
                OnPropertyChanged(nameof(ConnectionString));
        }
    }

    public string Username
    {
        get => _username;
        set
        {
            if (SetField(ref _username, value))
                OnPropertyChanged(nameof(ConnectionString));
        }
    }

    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    public string TableName
    {
        get => _tableName;
        set => SetField(ref _tableName, value);
    }

    public string TestMessage
    {
        get => _testMessage;
        private set => SetField(ref _testMessage, value);
    }

    public bool IsTestSuccessful
    {
        get => _isTestSuccessful;
        private set => SetField(ref _isTestSuccessful, value);
    }

    public bool IsTesting
    {
        get => _isTesting;
        private set => SetField(ref _isTesting, value);
    }

    public string DetailedError
    {
        get => _detailedError;
        private set
        {
            if (SetField(ref _detailedError, value))
                OnPropertyChanged(nameof(HasDetailedError));
        }
    }

    public bool HasDetailedError => !string.IsNullOrEmpty(DetailedError);

    public string CopyDetailsText { get; private set; } = string.Empty;
    public bool CanCopyDetails => !string.IsNullOrEmpty(CopyDetailsText);
    public ICommand CopyDetailsCommand { get; }

    public string ConnectionString
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Database)
                || string.IsNullOrWhiteSpace(Username))
                return string.Empty;
            return BuildConfig().BuildConnectionString(maskPassword: true);
        }
    }

    public ICommand TestConnectionCommand { get; }
    public ICommand SaveCommand { get; }

    private readonly Action<SybaseConnectionConfig> _saveAction;

    public SybaseConnectionViewModel(
        SybaseConnectionConfig? current,
        Action<SybaseConnectionConfig> saveAction)
    {
        _saveAction = saveAction;

        if (current != null)
        {
            _host = current.Host;
            _port = current.Port;
            _database = current.Database;
            _username = current.Username;
            _password = current.Password;
            _tableName = current.TableName;
        }

        TestConnectionCommand = new RelayCommand(async _ => await TestConnectionAsync(), _ => !IsTesting);
        SaveCommand = new RelayCommand(_ => Save(), _ => BuildConfig().IsValid);
        CopyDetailsCommand = new RelayCommand(_ =>
        {
            try { Clipboard.SetText(CopyDetailsText); }
            catch { /* Ignored: clipboard may not be available in all contexts */ }
        });
    }

    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestMessage = "Probando conexion...";
        IsTestSuccessful = false;

        var config = BuildConfig();
        try
        {
            var cs = config.BuildConnectionString();

            await using var connection = new OdbcConnection(cs);
            await connection.OpenAsync();

            IsTestSuccessful = true;
            TestMessage = "Conexion exitosa.";
        }
        catch (Exception ex)
        {
            IsTestSuccessful = false;
            TestMessage = $"Error: {ex.Message}";
            DetailedError = ex.ToString();
            var masked = config.BuildConnectionString(maskPassword: true);
            CopyDetailsText = $"ConnectionString:{Environment.NewLine}{masked}{Environment.NewLine}{Environment.NewLine}Error:{Environment.NewLine}{DetailedError}";
            OnPropertyChanged(nameof(CopyDetailsText));
            OnPropertyChanged(nameof(CanCopyDetails));
        }
        finally
        {
            IsTesting = false;
        }
    }

    private SybaseConnectionConfig BuildConfig()
    {
        return new SybaseConnectionConfig
        {
            Host = Host,
            Port = Port,
            Database = Database,
            Username = Username,
            Password = Password,
            TableName = TableName
        };
    }

    public void SetPasswordFromDialog(string password)
    {
        _password = password;
        OnPropertyChanged(nameof(Password));
    }

    private void Save()
    {
        _saveAction(BuildConfig());
    }
}